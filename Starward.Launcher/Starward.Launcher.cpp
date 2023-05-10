#define _CRT_SECURE_NO_WARNINGS

#include <iostream>
#include <filesystem>
#include <Windows.h>
#include "INIReader.h"

#pragma comment(linker, "/subsystem:windows /entry:wmainCRTStartup")

using namespace std::filesystem;



int wmain(int argc, wchar_t* argv[])
{
	std::wstring exe, arg = std::wstring(GetCommandLine()).substr(std::wstring(argv[0]).length() + 2);

	auto folder = path(argv[0]).parent_path();
	auto config = path(folder).append("config.ini");
	if (exists(config))
	{
		INIReader ini(config.string());
		if (ini.ParseError() == 0)
		{
			auto app_folder = ini.Get("", "app_folder", "");
			auto exe_name = ini.Get("", "exe_name", "Starward.exe");
			auto _exe = path(folder).append(app_folder).append(exe_name);
			if (exists(_exe))
			{
				exe = _exe.wstring();
			}
		}
	}

	if (!exe.length())
	{
		path _exe;
		file_time_type time;
		for (auto d : directory_iterator(folder))
		{
			if (d.is_directory())
			{
				auto _time = d.last_write_time();
				if (_time > time)
				{
					auto __exe = path(d).append(L"Starward.exe");
					if (exists(__exe))
					{
						_exe = __exe;
						time = _time;
					}
				}
			}
		}
		if (exists(_exe))
		{
			exe = _exe;
		}
	}

	if (exists(exe))
	{
		STARTUPINFO si;
		PROCESS_INFORMATION pi;
		ZeroMemory(&si, sizeof(si));
		si.cb = sizeof(si);
		ZeroMemory(&pi, sizeof(pi));
		CreateProcess(exe.c_str(), (LPWSTR)arg.c_str(), NULL, NULL, false, 0, NULL, NULL, &si, &pi);
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
	}
	else
	{
		SetProcessDPIAware();
		auto ok = MessageBox(NULL, L"Cannot run the app because some files not found.\r\nWould you like to download it now?\r\nhttps://github.com/Scighost/Starward", L"Starward", MB_ICONWARNING | MB_OKCANCEL);
		if (ok == IDOK)
		{
			ShellExecute(NULL, NULL, L"https://github.com/Scighost/Starward", NULL, NULL, SW_SHOWNORMAL);
		}
	}
}

