add_rules("mode.release", "mode.debug")

option("unity_dxc_dir")
    set_default(false)
    set_showmenu(true)
option_end()

local lc_options = {
    lc_dx_backend = true,
    lc_vk_backend = false,
    lc_cuda_backend = false,
    lc_metal_backend = false,
    lc_fallback_backend = false,
    lc_enable_tests = false,
    lc_enable_dsl = true,
    lc_enable_gui = false,
    lc_enable_imgui = false,
    lc_enable_osl = false,
    lc_enable_clangcxx = false,
    lc_enable_xir = false,
    lc_enable_py = false,
    lc_dx_cuda_interop = false,
    lc_vk_cuda_interop = false
}
if is_host("windows") then
    set_config("lc_toolchain", "llvm")
end
for k, v in pairs(lc_options) do
    set_config(k, v)
end

includes("compute")
if is_arch("x64", "x86_64", "arm64") then
	if is_mode("debug") then
		set_targetdir("bin/debug")
	else
		set_targetdir("bin/release")
	end
	includes("src")
else
	target("_lc_unity_illegal_env")
	set_kind("phony")
	on_load(function(target)
		utils.error("Illegal environment. Please check your compiler, architecture or platform.")
	end)
	target_end()
end
