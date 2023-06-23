#define _CRT_SECURE_NO_WARNINGS

#include <iostream>
#include <filesystem>
#include <Windows.h>
#include "INIReader.h"

#pragma comment(linker, "/subsystem:windows /entry:wmainCRTStartup")

using namespace std::filesystem;



int wmain(int argc, wchar_t* argv[])
{
	std::wstring run_exe, arg = std::wstring(GetCommandLine()).substr(std::wstring(argv[0]).length() + 2);

	auto base_folder = path(argv[0]).parent_path();
	auto version = path(base_folder).append("version.ini");
	if (exists(version))
	{
		INIReader ini(version.string());
		if (ini.ParseError() == 0)
		{
			auto app_folder = ini.Get("", "app_folder", "");
			if (app_folder.length())
			{
				auto exe_name = ini.Get("", "exe_name", "Starward.exe");
				auto target_exe = path(base_folder).append(app_folder).append(exe_name);
				if (exists(target_exe))
				{
					run_exe = target_exe.wstring();
				}
			}
		}
	}

	if (!run_exe.length())
	{
		path target_exe;
		file_time_type last_time;
		for (auto folder : directory_iterator(base_folder))
		{
			if (folder.is_directory())
			{
				auto exe = path(folder).append(L"Starward.exe");
				if (exists(exe))
				{
					auto time = last_write_time(exe);
					if (time > last_time)
					{
						target_exe = exe;
						last_time = time;
					}
				}
			}
		}
		if (exists(target_exe))
		{
			run_exe = target_exe.wstring();
		}
	}

	if (exists(run_exe))
	{
		STARTUPINFO si;
		PROCESS_INFORMATION pi;
		ZeroMemory(&si, sizeof(si));
		si.cb = sizeof(si);
		ZeroMemory(&pi, sizeof(pi));
		CreateProcess(run_exe.c_str(), (LPWSTR)arg.c_str(), NULL, NULL, false, 0, NULL, NULL, &si, &pi);
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);

		auto base_name = path(run_exe).parent_path().filename().wstring();
		for (auto folder : directory_iterator(base_folder))
		{
			auto folder_name = folder.path().filename().wstring();
			if (folder.is_directory() && folder_name.starts_with(L"app-") && folder_name.compare(base_name))
			{
				remove_all(folder);
			}
		}
	}
	else
	{
		SetProcessDPIAware();
		auto ok = MessageBox(NULL, L"Starward files not found.\r\nWould you like to download it now?\r\nhttps://github.com/Scighost/Starward", L"Starward", MB_ICONWARNING | MB_OKCANCEL);
		if (ok == IDOK)
		{
			ShellExecute(NULL, NULL, L"https://github.com/Scighost/Starward", NULL, NULL, SW_SHOWNORMAL);
		}
	}
}

