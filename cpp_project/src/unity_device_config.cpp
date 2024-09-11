#include "unity_device_config.h"
#include <luisa/core/stl/optional.h>
#ifdef LC_UNITY3D_ENABLE_DX12
#ifdef NDEBUG
#define ThrowIfFailed(x) (x)
#else
#define ThrowIfFailed(x)     \
    {                        \
        HRESULT hr_ = (x);   \
        assert(hr_ == S_OK); \
    }
#endif

using namespace Microsoft::WRL;
using namespace luisa;
using namespace luisa::compute;

luisa::string local_path;
luisa::string plugin_path() {
    char buffer[MAX_PATH] = {0};
    GetDllDirectoryA(MAX_PATH, buffer);
    luisa::string str(buffer);
    if (!str.empty() && *(str.end() - 1) != '\\' && *(str.end() - 1) != '\\') {
        str += '/';
    }
    return str;
}
class EnginePathIniter {
public:
    EnginePathIniter() {
        local_path = plugin_path();
    }
};
static EnginePathIniter enginePathIniter;
static IUnityGraphicsD3D12v5 *_unity_graphics{nullptr};
UnityDeviceConfig::UnityDeviceConfig() noexcept {
    uint32_t dxgiFactoryFlags = 0;
    ThrowIfFailed(CreateDXGIFactory2(dxgiFactoryFlags, IID_PPV_ARGS(_dxgi_factory.GetAddressOf())));
    _device = _unity_graphics->GetDevice();
    for (auto adapterIndex = 0u; _dxgi_factory->EnumAdapters1(adapterIndex, _adapter.GetAddressOf()) != DXGI_ERROR_NOT_FOUND; adapterIndex++) {
        DXGI_ADAPTER_DESC1 desc;
        _adapter->GetDesc1(&desc);
        auto uid = _device->GetAdapterLuid();
        if (memcmp(&desc.AdapterLuid, &uid, sizeof(LUID)) == 0) {
            break;
        }
        _adapter = nullptr;
    }
}
auto UnityDeviceConfig::CreateExternalDevice() noexcept -> luisa::optional<ExternalDevice> {
    return ExternalDevice{
        _device,
        _adapter.Get(),
        _dxgi_factory.Get()};
}
// queue is nullable
ID3D12CommandQueue *UnityDeviceConfig::CreateQueue(D3D12_COMMAND_LIST_TYPE type) noexcept {
    if (type == D3D12_COMMAND_LIST_TYPE_DIRECT)
        return _unity_graphics->GetCommandQueue();
    return nullptr;
}
bool UnityDeviceConfig::ExecuteCommandList(
    ID3D12CommandQueue *queue,
    ID3D12GraphicsCommandList *cmdList) noexcept {
    if (queue != _unity_graphics->GetCommandQueue()) return false;
    ID3D12CommandList *cmdListBase = cmdList;
    _unity_graphics->ExecuteCommandList(cmdList, _resource_states.size(), _resource_states.data());
    _resource_states.clear();
    return true;
}
void UnityDeviceConfig::regist_resource(
    ID3D12Resource *resource,
    D3D12_RESOURCE_STATES state) noexcept {
    _resource_states.emplace_back(UnityGraphicsD3D12ResourceState{
        resource,
        state,
        state});
}
LCPlugin::LCPlugin(luisa::unique_ptr<UnityDeviceConfig> &&config)
    : _lc_context(local_path), _unity_config(config.get()) {
    DeviceConfig device_config{
        .extension = std::move(config),
        .inqueue_buffer_limit = false};
    _lc_device = _lc_context.create_device("dx", &device_config);
    _lc_stream = _lc_device.create_stream(StreamTag::GRAPHICS);
    pt_component.init(_lc_device, _lc_stream);
}
LCPlugin::~LCPlugin() {
    _lc_stream << synchronize();
}
static luisa::unique_ptr<LCPlugin> plugin;
LCPlugin *LCPlugin::instance() {
    return plugin.get();
}
size_t LCPlugin::emplace(int event_id, luisa::span<const std::byte> data) {
    std::lock_guard lck{_event_mtx};
    if (_events.size() <= event_id) {
        _events.resize(event_id + 1);
    }
    auto &vec = _events[event_id].data;
    vec.clear();
    vec.push_back_uninitialized(data.size());
    std::memcpy(vec.data(), data.data(), data.size());
    return event_id;
}
auto LCPlugin::get(int id) -> Event & {
    std::lock_guard lck{_event_mtx};
    if (_events.size() > id) {
        return _events[id];
    } else {
        return *reinterpret_cast<Event *>(0);
    }
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces *unity_interface) {
    _unity_graphics = unity_interface->Get<IUnityGraphicsD3D12v5>();
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() {
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API LCPluginLoad() {
    if (!plugin)
        plugin = luisa::make_unique<LCPlugin>(luisa::make_unique<UnityDeviceConfig>());
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API LCPluginUnLoad() {
    plugin = nullptr;
}
// Implement this function in other module.
static void UNITY_INTERFACE_API OnRenderEvent(int eventID) {
    plugin->on_render_event(eventID);
}
extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc() {
    return OnRenderEvent;
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API emplace_data(int event_id, void *ptr, int size) {
    return plugin->emplace(event_id, luisa::span{reinterpret_cast<std::byte const *>(ptr), size_t(size)});
}
#endif