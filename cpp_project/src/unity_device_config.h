#pragma once

#include "i_unity_graphics.h"
#ifdef LC_UNITY3D_ENABLE_DX12
#include "i_unity_d3d12.h"
#include <d3d12.h>
#include <dxgi1_4.h>
#include <comdef.h>
#include <luisa/backends/ext/dx_config_ext.h>
#include <wrl/client.h>
#include <luisa/runtime/context.h>
#include <luisa/runtime/device.h>
#include <luisa/runtime/image.h>
#include <luisa/runtime/stream.h>
#include <luisa/runtime/shader.h>
#include <luisa/runtime/rtx/accel.h>
#include <luisa/runtime/rtx/mesh.h>
#include <luisa/runtime/buffer_arena.h>
using namespace luisa;
using namespace luisa::compute;
class UnityDeviceConfig : public DirectXDeviceConfigExt {
private:
    Microsoft::WRL::ComPtr<IDXGIAdapter1> _adapter;
    Microsoft::WRL::ComPtr<IDXGIFactory4> _dxgi_factory;
    ID3D12Device *_device{nullptr};
    vector<UnityGraphicsD3D12ResourceState> _resource_states;

public:
    [[nodiscard]] auto adapter() const noexcept { return _adapter.Get(); }
    [[nodiscard]] auto dxgi_factory() const noexcept { return _dxgi_factory.Get(); }
    [[nodiscard]] auto device() const noexcept { return _device; }
    UnityDeviceConfig() noexcept;
    ~UnityDeviceConfig() noexcept = default;
    ID3D12Device *GetDevice() noexcept override;
    IDXGIAdapter1 *GetAdapter() noexcept override;
    IDXGIFactory4 *GetDXGIFactory() noexcept override;
    ID3D12CommandQueue *CreateQueue(D3D12_COMMAND_LIST_TYPE type) noexcept override;
    bool ExecuteCommandList(
        ID3D12CommandQueue *queue,
        ID3D12GraphicsCommandList *cmdList) noexcept override;
    void regist_resource(
        ID3D12Resource *resource,
        D3D12_RESOURCE_STATES state) noexcept;
};
struct CreateRTData {
    ID3D12Resource *ptr;
    ID3D12Resource *depthPtr;
    PixelStorage storage;
    float invvp[16];
    float camera_pos[3];
    bool resetFrame;
};
class PathTracingComponent {
public:
    struct Arg {
        float4x4 invvp;
        float3 cam_pos;
        uint frame;
    };
    luisa::optional<BufferArena> buffer_arena;
    vector<Mesh> meshes;
    Accel accel;
    BindlessArray heap;
    Shader2D<Image<uint>> init_sampler_shader;
    Shader2D<Image<uint>, Image<float>, Image<float>, Image<float>, Arg> shader;
    Image<uint> seed_image;
    Image<float> color_image;
    uint frame = 0;
    void init(Device &device, Stream &stream);
    void execute(Device &device, ImageView<float> tex, ImageView<float> depth_tex, CommandList &cmdlist, CreateRTData const &data);
};

class LCPlugin {
public:
    struct Event {
        vector<std::byte> data;
    };
private:
    Context _lc_context;
    Device _lc_device;
    Stream _lc_stream;
    UnityDeviceConfig *_unity_config;
    vector<Event> _events;
    vector<int> _empty_elements;
    PathTracingComponent pt_component;
    std::mutex _event_mtx;

public:
    size_t emplace(int event_id, span<const std::byte> data);
    Event &get(int id);
    void on_render_event(int index);
    auto const &device() const { return _lc_device; }
    auto const &stream() const { return _lc_stream; }
    static LCPlugin *instance();
    auto unity_config() const { return _unity_config; }
    LCPlugin(unique_ptr<UnityDeviceConfig> &&config);
    ~LCPlugin();
};
#endif