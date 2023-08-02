set_xmakever("2.8.1")
add_rules("mode.release", "mode.debug")

option("lc_path")
set_default(false)
set_showmenu(true)
before_check(function(option)
	local function invalid_str(s)
		return type(s) ~= "string" or s:len() == 0
	end
	if invalid_str(option:enabled()) then
		utils.error("Must have luisa-compute's path!")
	end
end)
option_end()

if is_arch("x64", "x86_64", "arm64") then
	includes("scripts/xmake_func.lua")
	local lc_path = get_config("lc_path")
	if lc_path then
		includes(path.join(lc_path, "config/xmake_rules.lua"))
	end
	function load_lc()
		if lc_path then
			set_values("lc_is_public", true)
			set_values("lc_dir", lc_path)
			add_rules("add_lc_includedirs", "add_lc_defines")
		end
		add_rules("link_lc", "copy_dll")
	end
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
