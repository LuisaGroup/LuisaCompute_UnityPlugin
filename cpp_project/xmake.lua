add_rules("mode.release", "mode.debug")
local lc_options = {
    cpu_backend = false,
    cuda_backend = false,
    dx_backend = true,
    enable_cuda = false,
    enable_api = false,
    enable_clangcxx = true,
    enable_dsl = true,
    enable_gui = true,
    enable_osl = false,
    enable_ir = false,
    enable_tests = false,
    metal_backend = false
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
