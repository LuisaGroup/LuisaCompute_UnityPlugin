target("lc-unity3d")
_config_project({
    project_kind = "shared"
})
add_deps("lc-runtime", "lc-dsl", "lc-vstl")
add_deps("lc-backend-dx", {inherit = false, links = false})
set_pcxxheader("pch.h")
add_files("**.cpp")
add_syslinks("DXGI")
if is_plat("windows") then
    add_defines("LC_UNITY3D_ENABLE_DX12", {
        public = true
    })
end
after_build(function(target)
    if not target:is_plat("windows") then
        return
    end

    local dxc_dir = get_config("unity_dxc_dir")
    if not dxc_dir then
        local version_file = path.join(os.projectdir(), "../ProjectSettings/ProjectVersion.txt")
        local version_text = io.readfile(version_file)
        local unity_version = version_text and version_text:match("m_EditorVersion:%s*([^\r\n]+)")
        local program_files = os.getenv("ProgramFiles")
        if unity_version and program_files then
            dxc_dir = path.join(program_files, "Unity/Hub/Editor", unity_version, "Editor/Data/Tools")
        end
    end
    if not dxc_dir or not os.isdir(dxc_dir) then
        raise("Unity DXC directory not found. Configure it with --unity_dxc_dir=<Unity Editor/Data/Tools>.")
    end

    for _, filename in ipairs({"dxcompiler.dll", "dxil.dll"}) do
        local source = path.join(dxc_dir, filename)
        if not os.isfile(source) then
            raise(filename .. " not found in " .. dxc_dir)
        end
        os.cp(source, target:targetdir())
    end

    local plugin_dir = path.join(os.projectdir(), "../Assets/Plugins")
    os.mkdir(plugin_dir)
    os.cp(path.join(target:targetdir(), "*.dll"), plugin_dir)
end)
target_end()
