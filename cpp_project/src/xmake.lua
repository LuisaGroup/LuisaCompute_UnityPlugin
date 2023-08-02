target("lc-unity3d")
_config_project({
    project_kind = "shared"
})
load_lc()
set_pcxxheader("pch.h")
add_files("**.cpp")
add_syslinks("DXGI")
if is_plat("windows") then
    add_defines("LC_UNITY3D_ENABLE_DX12", {
        public = true
    })
end
target_end()