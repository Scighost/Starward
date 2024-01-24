#define _CRT_SECURE_NO_WARNINGS

#include <iostream>
#include <filesystem>
#include <Windows.h>
#include "INIReader.h"

#pragma comment(linker, "/subsystem:windows /entry:wmainCRTStartup")

using namespace std::filesystem;


HANDLE stdOut;


void Log(std::wstring output)
{
	if (stdOut)
	{
		SYSTEMTIME time;
		GetSystemTime(&time);
		std::wstring timeStr = std::format(L"[{:02}:{:02}:{:02}.{:03}] ", time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
		WriteConsole(stdOut, timeStr.c_str(), timeStr.length(), NULL, NULL);
		WriteConsole(stdOut, output.c_str(), output.length(), NULL, NULL);
		WriteConsole(stdOut, L"\r\n", 2, NULL, NULL);
	}
}


int wmain(int argc, wchar_t* argv[])
{
	for (size_t i = 0; i < argc; i++)
	{
		if (!wcscmp(argv[i], L"--trace"))
		{
			AllocConsole();
		}
	}

	stdOut = GetStdHandle(STD_OUTPUT_HANDLE);

	std::wstring run_exe;

	auto base_folder = path(argv[0]).parent_path();
	auto version = path(base_folder).append("version.ini");
	if (exists(version))
	{
		INIReader ini(version.string());
		if (ini.ParseError() == 0)
		{
			auto exe_path = ini.Get("", "exe_path", "");
			if (exe_path.length())
			{
				auto target_exe = path(base_folder).append(exe_path);
				if (exists(target_exe))
				{
					run_exe = target_exe.wstring();
				}
			}
		}
	}
	else
	{
		Log(L"version.ini not found");
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
		run_exe = target_exe.wstring();
	}

	Log(L"run_exe: " + run_exe);

	if (run_exe.length())
	{
		std::wstring arg = std::wstring(GetCommandLine()).substr(std::wstring(argv[0]).length() + 2);
		Log(L"arg: " + arg);
		STARTUPINFO si;
		PROCESS_INFORMATION pi;
		ZeroMemory(&si, sizeof(si));
		si.cb = sizeof(si);
		ZeroMemory(&pi, sizeof(pi));
		Log(L"Starting process");
		CreateProcess(run_exe.c_str(), (LPWSTR)arg.c_str(), NULL, NULL, false, 0, NULL, NULL, &si, &pi);
		Log(std::format(L"Process started ({})", GetProcessId(pi.hProcess)));
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);

		auto base_name = path(run_exe).parent_path().filename().wstring();
		for (auto folder : directory_iterator(base_folder))
		{
			auto folder_name = folder.path().filename().wstring();
			if (folder.is_directory() && folder_name.starts_with(L"app-") && folder_name.compare(base_name))
			{
				Log(std::format(L"Removing old version: {}", folder_name));
				remove_all(folder);
			}
		}
	}
	else
	{
		Log(L"Starward.exe not found");
		SetProcessDPIAware();
		auto ok = MessageBox(NULL, L"Starward files not found.\r\nWould you like to download it now?\r\nhttps://github.com/Scighost/Starward", L"Starward", MB_ICONWARNING | MB_OKCANCEL);
		if (ok == IDOK)
		{
			ShellExecute(NULL, NULL, L"https://github.com/Scighost/Starward", NULL, NULL, SW_SHOWNORMAL);
		}
	}

	if (stdOut)
	{
		Log(L"Wait for 10s to exit...");
		Sleep(10000);
	}
}

