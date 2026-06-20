#define _CRT_SECURE_NO_WARNINGS

#include <filesystem>
#include <string>
#include <Windows.h>
#include "INIReader.h"

#pragma comment(linker, "/subsystem:windows /entry:wmainCRTStartup")


int wmain(int argc, wchar_t* argv[])
{
	std::filesystem::path run_exe;

	const std::filesystem::path base_folder = std::filesystem::path(argv[0]).parent_path();
	const std::filesystem::path version_file = base_folder / "version.ini";
	if (std::filesystem::exists(version_file))
	{
		INIReader ini(version_file.string());
		if (ini.ParseError() == 0)
		{
			std::string version_text = ini.Get("", "version", "");
			if (!version_text.empty())
			{
				std::filesystem::path target_exe = base_folder / ("app-" + version_text) / "Starward.exe";
				if (std::filesystem::exists(target_exe))
				{
					run_exe = target_exe;
				}
			}
		}
	}

	if (run_exe.empty())
	{
		std::filesystem::path target_exe;
		std::filesystem::file_time_type last_time{};
		bool found = false;
		for (const std::filesystem::directory_entry& folder : std::filesystem::directory_iterator(base_folder, std::filesystem::directory_options::skip_permission_denied))
		{
			if (folder.is_directory())
			{
				std::wstring folder_name = folder.path().filename().wstring();
				if (!folder_name.starts_with(L"app-"))
				{
					continue;
				}

				std::filesystem::path exe = folder.path() / L"Starward.exe";
				if (std::filesystem::exists(exe))
				{
					std::filesystem::file_time_type time = std::filesystem::last_write_time(exe);
					if (!found || time > last_time)
					{
						target_exe = exe;
						last_time = time;
						found = true;
					}
				}
			}
		}
		run_exe = target_exe;
	}

	if (!run_exe.empty())
	{
		STARTUPINFOW si{};
		si.cb = sizeof(si);
		PROCESS_INFORMATION pi{};
		std::wstring arg = std::wstring(GetCommandLine()).substr(std::wstring(argv[0]).length() + 2);
		if (CreateProcess(run_exe.c_str(), (LPWSTR)arg.c_str(), NULL, NULL, false, 0, NULL, NULL, &si, &pi))
		{
			CloseHandle(pi.hProcess);
			CloseHandle(pi.hThread);

			std::wstring base_name = run_exe.parent_path().filename().wstring();
			for (const std::filesystem::directory_entry& folder : std::filesystem::directory_iterator(base_folder, std::filesystem::directory_options::skip_permission_denied))
			{
				std::wstring folder_name = folder.path().filename().wstring();
				if (folder.is_directory() && folder_name.starts_with(L"app-") && folder_name != base_name)
				{
					std::filesystem::remove_all(folder);
				}
			}
		}
	}
	else
	{
		SetProcessDPIAware();
		int ok = MessageBox(NULL, L"Starward files not found.\r\nWould you like to download it now?\r\nhttps://github.com/Scighost/Starward", L"Starward", MB_ICONWARNING | MB_OKCANCEL);
		if (ok == IDOK)
		{
			ShellExecute(NULL, NULL, L"https://github.com/Scighost/Starward", NULL, NULL, SW_SHOWNORMAL);
		}
	}
}
