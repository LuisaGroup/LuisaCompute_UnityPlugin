#pragma once
#ifdef LC_UNITY3D_ENABLE_DX12
#include "i_unity_d3d12.h"
#include <d3d12.h>
#include <dxgi1_4.h>
#include <comdef.h>
#define LUISA_ENABLE_DSL
#include <luisa/luisa-compute.h>
#include <luisa/backends/ext/dx_config_ext.h>
#include <wrl/client.h>
#endif
#include "cornell_box.h"
#include "tiny_obj_loader.h"