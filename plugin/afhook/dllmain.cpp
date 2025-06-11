#include "targetver.h"
#include <windows.h>
#include <string>

#include "afhook.h"
#include "CHookEngine.h"
#include "CLogger.h"
#include "CResourceManager.h"
#include "CUtils.h"

CHookEngine g_hookEngine;
CFileLogger g_logger;
CResourceManager g_resources;

std::wstring GetTempPackagePath(const std::wstring& originalPath)
{
	// Find last dot position
	size_t lastDotPos = originalPath.rfind(L'.');
	// Find last path separator position
	size_t lastSlashPos = originalPath.find_last_of(L"/\\");

	// Only treat as extension if:
	// 1. There is a dot
	// 2. The dot is after the last path separator
	// 3. The dot isn't the first character of the filename
	if (lastDotPos != std::wstring::npos &&
		(lastSlashPos == std::wstring::npos || lastDotPos > lastSlashPos) &&
		lastDotPos != (lastSlashPos + 1))
	{
		// Insert "_tmp" before the extension
		return originalPath.substr(0, lastDotPos) + L"_tmp" + originalPath.substr(lastDotPos);
	}

	// No valid extension found, just append "_tmp"
	return originalPath + L"_tmp";
}

bool SafeSavePackage(CResourceManager& resources, const std::wstring& path)
{
	std::wstring tempPath = GetTempPackagePath(path);

	// Save to temporary file first
	resources.SavePackage(tempPath);

	// Verify the temp file was created
	if (GetFileAttributesW(tempPath.c_str()) == INVALID_FILE_ATTRIBUTES)
	{
		return false;
	}

	// Delete the original file if it exists
	if (GetFileAttributesW(path.c_str()) != INVALID_FILE_ATTRIBUTES)
	{
		if (!DeleteFileW(path.c_str()))
		{
			// Failed to delete original file
			DeleteFileW(tempPath.c_str()); // Clean up temp file
			return false;
		}
	}

	// Rename temp file to original name
	if (!MoveFileW(tempPath.c_str(), path.c_str()))
	{
		return false;
	}

	return true;
}

bool SafeLoadPackage(CResourceManager& resources, const std::wstring& path)
{
	std::wstring tempPath = GetTempPackagePath(path);

	// Check if original file exists
	if (GetFileAttributesW(path.c_str()) != INVALID_FILE_ATTRIBUTES)
	{
		resources.LoadPackage(path);
		return true;
	}

	// If original doesn't exist but temp does, use the temp file
	if (GetFileAttributesW(tempPath.c_str()) != INVALID_FILE_ATTRIBUTES)
	{
		// Try to rename temp to original first
		if (MoveFileW(tempPath.c_str(), path.c_str()))
		{
			resources.LoadPackage(path);
		}
		else
		{
			// If rename failed, just load from temp
			resources.LoadPackage(tempPath);
		}
		return true;
	}

	// Neither file exists - still try to load from original path
	resources.LoadPackage(path);
	return true;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	if (ul_reason_for_call == DLL_PROCESS_ATTACH)
	{
		g_logger.Open(CUtils::GetGameDirectory(L"\\afhook.log"));

		g_resources.SetLogger(&g_logger);

		std::wstring packagePath = CUtils::GetGameDirectory(L"\\afhook.pkg");
		SafeLoadPackage(g_resources, packagePath);

		g_hookEngine.SetLogger(&g_logger);
		g_hookEngine.SetResourceManager(&g_resources);

		return g_hookEngine.HookGame();
	}
	else if (ul_reason_for_call == DLL_PROCESS_DETACH)
	{
		std::wstring packagePath = CUtils::GetGameDirectory(L"\\afhook.pkg");
		SafeSavePackage(g_resources, packagePath);

		return g_hookEngine.UnhookGame();
	}
	else if (ul_reason_for_call == DLL_THREAD_DETACH)
	{
		if (g_resources.IsModified())
		{
			std::wstring packagePath = CUtils::GetGameDirectory(L"\\afhook.pkg");
			SafeSavePackage(g_resources, packagePath);
		}
	}

	return TRUE;
}