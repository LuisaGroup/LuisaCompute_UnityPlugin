#include "unity_device_config.h"
#include <luisa/core/stl/optional.h>
#include <luisa/backends/ext/native_resource_ext.hpp>
#include <luisa/runtime/image.h>
#include <luisa/core/logging.h>
#include <luisa/runtime/raster/depth_buffer.h>
#include <luisa/dsl/sugar.h>
#include "tiny_obj_loader.h"
#include "cornell_box.h"
using namespace luisa::compute;
using namespace luisa;
// see /src/backends/dx/DXApi/ext.h
struct NativeTextureDesc {
    D3D12_RESOURCE_STATES initState;
    DXGI_FORMAT custom_format;
    bool allowUav;
};

struct Onb {
    float3 tangent;
    float3 binormal;
    float3 normal;
};

LUISA_STRUCT(Onb, tangent, binormal, normal) {
    [[nodiscard]] Float3 to_world(Expr<float3> v) const noexcept {
        return v.x * tangent + v.y * binormal + v.z * normal;
    }
};
LUISA_STRUCT(PathTracingComponent::Arg, invvp, cam_pos, frame) {};

void PathTracingComponent ::init(Device &device, Stream &stream) {
    buffer_arena.emplace(device);
    tinyobj::ObjReaderConfig obj_reader_config;
    obj_reader_config.triangulate = true;
    obj_reader_config.vertex_color = false;
    tinyobj::ObjReader obj_reader;
    if (!obj_reader.ParseFromString(obj_string, "", obj_reader_config)) {
        luisa::string_view error_message = "unknown error.";
        if (auto &&e = obj_reader.Error(); !e.empty()) { error_message = e; }
        LUISA_ERROR_WITH_LOCATION("Failed to load OBJ file: {}", error_message);
    }
    if (auto &&e = obj_reader.Warning(); !e.empty()) {
        LUISA_WARNING_WITH_LOCATION("{}", e);
    }

    auto &&p = obj_reader.GetAttrib().vertices;
    luisa::vector<float3> vertices;
    vertices.reserve(p.size() / 3u);
    for (uint i = 0u; i < p.size(); i += 3u) {
        vertices.emplace_back(make_float3(
            p[i + 0u], p[i + 1u], p[i + 2u]));
    }

    auto vertex_buffer = buffer_arena->allocate<float3>(vertices.size());
    stream << vertex_buffer.copy_from(vertices.data());
    accel = device.create_accel();
    heap = device.create_bindless_array();
    for (auto &&shape : obj_reader.GetShapes()) {
        uint index = static_cast<uint>(meshes.size());
        std::vector<tinyobj::index_t> const &t = shape.mesh.indices;
        uint triangle_count = t.size() / 3u;
        LUISA_INFO(
            "Processing shape '{}' at index {} with {} triangle(s).",
            shape.name, index, triangle_count);
        luisa::vector<uint> indices;
        indices.reserve(t.size());
        for (tinyobj::index_t i : t) { indices.emplace_back(i.vertex_index); }
        auto triangle_buffer = buffer_arena->allocate<Triangle>(triangle_count);
        Mesh &mesh = meshes.emplace_back(device.create_mesh(vertex_buffer, triangle_buffer));
        heap.emplace_on_update(index, triangle_buffer);
        stream << triangle_buffer.copy_from(indices.data())
               << mesh.build();
        accel.emplace_back(mesh, make_float4x4(1.0f));
    }
    stream << heap.update()
           << accel.build()
           << synchronize();
    Constant materials{
        make_float3(0.725f, 0.710f, 0.680f),// floor
        make_float3(0.725f, 0.710f, 0.680f),// ceiling
        make_float3(0.725f, 0.710f, 0.680f),// back wall
        make_float3(0.140f, 0.450f, 0.091f),// right wall
        make_float3(0.630f, 0.065f, 0.050f),// left wall
        make_float3(0.725f, 0.710f, 0.680f),// short box
        make_float3(0.725f, 0.710f, 0.680f),// tall box
        make_float3(0.000f, 0.000f, 0.000f),// light
    };

    Callable linear_to_srgb = [&](Var<float3> x) noexcept {
        return saturate(select(1.055f * pow(x, 1.0f / 2.4f) - 0.055f,
                               12.92f * x,
                               x <= 0.00031308f));
    };

    Callable tea = [](UInt v0, UInt v1) noexcept {
        UInt s0 = def(0u);
        for (uint n = 0u; n < 4u; n++) {
            s0 += 0x9e3779b9u;
            v0 += ((v1 << 4) + 0xa341316cu) ^ (v1 + s0) ^ ((v1 >> 5u) + 0xc8013ea4u);
            v1 += ((v0 << 4) + 0xad90777du) ^ (v0 + s0) ^ ((v0 >> 5u) + 0x7e95761eu);
        }
        return v0;
    };

    Kernel2D make_sampler_kernel = [&](ImageUInt seed_image) noexcept {
        UInt2 p = dispatch_id().xy();
        UInt state = tea(p.x, p.y);
        seed_image.write(p, make_uint4(state));
    };

    Callable lcg = [](UInt &state) noexcept {
        constexpr uint lcg_a = 1664525u;
        constexpr uint lcg_c = 1013904223u;
        state = lcg_a * state + lcg_c;
        return cast<float>(state & 0x00ffffffu) *
               (1.0f / static_cast<float>(0x01000000u));
    };

    Callable make_onb = [](const Float3 &normal) noexcept {
        Float3 binormal = normalize(ite(
            abs(normal.x) > abs(normal.z),
            make_float3(-normal.y, normal.x, 0.0f),
            make_float3(0.0f, -normal.z, normal.y)));
        Float3 tangent = normalize(cross(binormal, normal));
        return def<Onb>(tangent, binormal, normal);
    };

    Callable generate_ray = [](Float2 p) noexcept {
        static constexpr float fov = radians(27.8f);
        static constexpr float3 origin = make_float3(-0.01f, 0.995f, 5.0f);
        Float3 pixel = origin + make_float3(p * tan(0.5f * fov), -1.0f);
        Float3 direction = normalize(pixel - origin);
        return make_ray(origin, direction);
    };

    Callable cosine_sample_hemisphere = [](Float2 u) noexcept {
        Float r = sqrt(u.x);
        Float phi = 2.0f * constants::pi * u.y;
        return make_float3(r * cos(phi), r * sin(phi), sqrt(1.0f - u.x));
    };

    Callable balanced_heuristic = [](Float pdf_a, Float pdf_b) noexcept {
        return pdf_a / max(pdf_a + pdf_b, 1e-4f);
    };
    Kernel2D raytracing_kernel = [&](ImageVar<uint> seed_image, ImageVar<float> image, ImageVar<float> display_image, ImageVar<float> depth_image, Var<Arg> arg) noexcept {
        UInt2 coord = dispatch_id().xy();
        UInt state = seed_image.read(coord).x;
        Float rx = lcg(state);
        Float ry = lcg(state);
        Float2 uv = (make_float2(coord) + make_float2(rx, ry)) / make_float2(dispatch_size().xy());
        Float4 proj = make_float4(uv * 2.f - 1.f, 1.f, 1.f);
        proj.y *= -1.f;
        Float4 world_pos = arg.invvp * proj;
        world_pos /= world_pos.w;
        auto ray = make_ray(arg.cam_pos, normalize(world_pos.xyz() - arg.cam_pos));
        Float3 radiance = def(make_float3(0.0f));
        Float3 beta = def(make_float3(1.0f));
        Float pdf_bsdf = def(0.0f);
        constexpr float3 light_position = make_float3(-0.24f, 1.98f, 0.16f);
        constexpr float3 light_u = make_float3(-0.24f, 1.98f, -0.22f) - light_position;
        constexpr float3 light_v = make_float3(0.23f, 1.98f, 0.16f) - light_position;
        constexpr float3 light_emission = make_float3(17.0f, 12.0f, 4.0f);
        Float light_area = length(cross(light_u, light_v));
        Float3 light_normal = normalize(cross(light_u, light_v));
        Float proj_depth = depth_image.read(coord).x;
        Bool hitted;
        $for (depth, 10u) {
            // trace
            Var<TriangleHit> hit = accel->trace_closest(ray);
            $if (hit->miss()) { $break; };
            $if (depth == 0) {
                proj = make_float4(uv * 2.f - 1.f, proj_depth, 1.f);
                world_pos = arg.invvp * proj;
                world_pos /= world_pos.w;
                $if (distance(world_pos.xyz(), arg.cam_pos) <= hit.committed_ray_t) {
                    $break;
                };
            };
            hitted = true;
            Var<Triangle> triangle = heap->buffer<Triangle>(hit.inst).read(hit.prim);
            Float3 p0 = vertex_buffer->read(triangle.i0);
            Float3 p1 = vertex_buffer->read(triangle.i1);
            Float3 p2 = vertex_buffer->read(triangle.i2);
            Float3 p = hit->interpolate(p0, p1, p2);
            Float3 n = normalize(cross(p1 - p0, p2 - p0));
            Float cos_wi = dot(-ray->direction(), n);
            $if (cos_wi < 1e-4f) { $break; };

            // hit light
            $if (hit.inst == static_cast<uint>(meshes.size() - 1u)) {
                $if (depth == 0u) {
                    radiance += light_emission;
                }
                $else {
                    Float pdf_light = length_squared(p - ray->origin()) / (light_area * cos_wi);
                    Float mis_weight = balanced_heuristic(pdf_bsdf, pdf_light);
                    radiance += mis_weight * beta * light_emission;
                };
                $break;
            };

            // sample light
            Float ux_light = lcg(state);
            Float uy_light = lcg(state);
            Float3 p_light = light_position + ux_light * light_u + uy_light * light_v;
            Float3 pp = offset_ray_origin(p, n);
            Float3 pp_light = offset_ray_origin(p_light, light_normal);
            Float d_light = distance(pp, pp_light);
            Float3 wi_light = normalize(pp_light - pp);
            Var<Ray> shadow_ray = make_ray(offset_ray_origin(pp, n), wi_light, 0.f, d_light);
            Bool occluded = accel->trace_any(shadow_ray);
            Float cos_wi_light = dot(wi_light, n);
            Float cos_light = -dot(light_normal, wi_light);
            Float3 albedo = materials.read(hit.inst);
            $if (!occluded & cos_wi_light > 1e-4f & cos_light > 1e-4f) {
                Float pdf_light = (d_light * d_light) / (light_area * cos_light);
                Float pdf_bsdf = cos_wi_light * inv_pi;
                Float mis_weight = balanced_heuristic(pdf_light, pdf_bsdf);
                Float3 bsdf = albedo * inv_pi * cos_wi_light;
                radiance += beta * bsdf * mis_weight * light_emission / max(pdf_light, 1e-4f);
            };

            // sample BSDF
            Var<Onb> onb = make_onb(n);
            Float ux = lcg(state);
            Float uy = lcg(state);
            Float3 new_direction = onb->to_world(cosine_sample_hemisphere(make_float2(ux, uy)));
            ray = make_ray(pp, new_direction);
            beta *= albedo;
            pdf_bsdf = cos_wi * inv_pi;

            // rr
            Float l = dot(make_float3(0.212671f, 0.715160f, 0.072169f), beta);
            $if (l == 0.0f) { $break; };
            Float q = max(l, 0.05f);
            Float r = lcg(state);
            $if (r >= q) { $break; };
            beta *= 1.0f / q;
        };
        $if (any(dsl::isnan(radiance))) { radiance = make_float3(0.0f); };
        Float4 result = make_float4(radiance, select(0.f, 1.f, hitted));
        $if (arg.frame > 0) {
            result = lerp(image.read(coord), result, 1.f / (arg.frame + 1.f));
        };
        seed_image.write(coord, make_uint4(state));
        image.write(coord, result);
        Float3 display = lerp(display_image.read(coord).xyz(), result.xyz(), result.w);
        display_image.write(coord, make_float4(display, 1.f));
    };
    shader = device.compile(raytracing_kernel);
    init_sampler_shader = device.compile(make_sampler_kernel);
}
void PathTracingComponent::execute(Device &device, ImageView<float> tex, ImageView<float> depth_tex, CommandList &cmdlist, CreateRTData const &data) {
    if (seed_image && (any(seed_image.size() != tex.size()))) {
        cmdlist.add_callback([seed_image = std::move(seed_image)]() {});
    }
    if (!seed_image) {
        seed_image = device.create_image<uint>(PixelStorage::INT1, tex.size());
        cmdlist << init_sampler_shader(seed_image).dispatch(tex.size());
    }
    if (color_image && (any(color_image.size() != tex.size()))) {
        cmdlist.add_callback([color_image = std::move(color_image)]() {});
    }
    frame++;
    if (!color_image) {
        color_image = device.create_image<float>(PixelStorage::FLOAT4, tex.size());
        frame = 0;
    }
    if (data.resetFrame) {
        frame = 0;
    }
    Arg arg{
        .cam_pos = make_float3(data.camera_pos[0], data.camera_pos[1], data.camera_pos[2]),
        .frame = frame};
    std::memcpy(&arg.invvp, &data.invvp, sizeof(float4x4));
    cmdlist << shader(seed_image, color_image, tex, depth_tex, arg).dispatch(tex.size());
}

void LCPlugin::on_render_event(int index) {
    auto &event = get(index);
    if (&event == nullptr) {
        return;
    }

    auto native_ext = _lc_device.extension<NativeResourceExt>();
    auto data_ptr = reinterpret_cast<CreateRTData const *>(event.data.data());
    auto tex_desc = data_ptr->ptr->GetDesc();
    NativeTextureDesc native_tex_desc{
        .initState = D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
        .custom_format = DXGI_FORMAT_UNKNOWN,
        .allowUav = true};
    _unity_config->regist_resource(data_ptr->ptr, D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
    auto tex = native_ext->create_native_image<float>(data_ptr->ptr, tex_desc.Width, tex_desc.Height, data_ptr->storage, 1, &native_tex_desc);
    D3D12_RESOURCE_STATES depth_state = D3D12_RESOURCE_STATE_DEPTH_READ;
    auto depth_tex = native_ext->create_native_depth_buffer(data_ptr->depthPtr, DepthFormat::D32S8A24, tex_desc.Width, tex_desc.Height, &depth_state);
    CommandList cmdlist;
    pt_component.execute(_lc_device, tex.view(), depth_tex.to_img(), cmdlist, *data_ptr);
    cmdlist.add_callback([tex = std::move(tex), depth_tex = std::move(depth_tex)]() {});
    _lc_stream << cmdlist.commit();
}