using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace FiveMQuantumTweaker2026.Utils
{
	/// <summary>
	/// Windows API Helper - Sichere Wrapper für Windows API Aufrufe
	/// </summary>
	public static class WinAPIHelper
	{
		private static readonly Logger _logger = Logger.CreateLogger();

		// ============================================
		// KERNEL32.DLL IMPORTS
		// ============================================

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetSystemTimes(
			out FILETIME lpIdleTime,
			out FILETIME lpKernelTime,
			out FILETIME lpUserTime);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint GetTickCount();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern ulong GetTickCount64();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryPerformanceFrequency(out long lpFrequency);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint GetCurrentProcessorNumber();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessTimes(
			IntPtr hProcess,
			out FILETIME lpCreationTime,
			out FILETIME lpExitTime,
			out FILETIME lpKernelTime,
			out FILETIME lpUserTime);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetProcessHeap();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsProcessorFeaturePresent(int ProcessorFeature);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int GetSystemFirmwareTable(
			uint FirmwareTableProviderSignature,
			uint FirmwareTableID,
			[Out] byte[] pFirmwareTableBuffer,
			uint BufferSize);

		// ============================================
		// ADVAPI32.DLL IMPORTS (Security & Registry)
		// ============================================

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(
			IntPtr ProcessHandle,
			uint DesiredAccess,
			out IntPtr TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetTokenInformation(
			IntPtr TokenHandle,
			TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation,
			uint TokenInformationLength,
			out uint ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegOpenKeyEx(
			IntPtr hKey,
			string lpSubKey,
			uint ulOptions,
			uint samDesired,
			out IntPtr phkResult);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern uint RegCloseKey(IntPtr hKey);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegQueryValueEx(
			IntPtr hKey,
			string lpValueName,
			IntPtr lpReserved,
			out uint lpType,
			IntPtr lpData,
			ref uint lpcbData);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegSetValueEx(
			IntPtr hKey,
			string lpValueName,
			uint Reserved,
			uint dwType,
			IntPtr lpData,
			uint cbData);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AdjustTokenPrivileges(
			IntPtr TokenHandle,
			[MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
			ref TOKEN_PRIVILEGES NewState,
			uint BufferLength,
			IntPtr PreviousState,
			IntPtr ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeValue(
			string lpSystemName,
			string lpName,
			out LUID lpLuid);

		// ============================================
		// USER32.DLL IMPORTS
		// ============================================

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetDoubleClickTime();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetSystemMetrics(int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SystemParametersInfo(
			uint uiAction,
			uint uiParam,
			IntPtr pvParam,
			uint fWinIni);

		// ============================================
		// POWRPROF.DLL IMPORTS (Power Management)
		// ============================================

		[DllImport("powrprof.dll", SetLastError = true)]
		public static extern uint PowerGetActiveScheme(
			IntPtr UserRootPowerKey,
			out IntPtr ActivePolicyGuid);

		[DllImport("powrprof.dll", SetLastError = true)]
		public static extern uint PowerSetActiveScheme(
			IntPtr UserRootPowerKey,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid);

		[DllImport("powrprof.dll", SetLastError = true)]
		public static extern uint PowerReadACValue(
			IntPtr RootPowerKey,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
			[MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
			out uint Type,
			IntPtr Buffer,
			ref uint BufferSize);

		// ============================================
		// NTDSAPI.DLL IMPORTS (TPM & Security)
		// ============================================

		[DllImport("ntdsapi.dll", SetLastError = true)]
		public static extern uint DsGetDcName(
			string ComputerName,
			string DomainName,
			IntPtr DomainGuid,
			string SiteName,
			uint Flags,
			out IntPtr DomainControllerInfo);

		// ============================================
		// STRUCTS & ENUMS
		// ============================================

		[StructLayout(LayoutKind.Sequential)]
		public struct FILETIME
		{
			public uint dwLowDateTime;
			public uint dwHighDateTime;

			public ulong ToUInt64()
			{
				return ((ulong)dwHighDateTime << 32) | dwLowDateTime;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TOKEN_PRIVILEGES
		{
			public uint PrivilegeCount;
			public LUID_AND_ATTRIBUTES Privileges;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;
			public uint Attributes;
		}

		public enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId,
			TokenGroupsAndPrivileges,
			TokenSessionReference,
			TokenSandBoxInert,
			TokenAuditPolicy,
			TokenOrigin,
			TokenElevationType,
			TokenLinkedToken,
			TokenElevation,
			TokenHasRestrictions,
			TokenAccessInformation,
			TokenVirtualizationAllowed,
			TokenVirtualizationEnabled,
			TokenIntegrityLevel,
			TokenUIAccess,
			TokenMandatoryPolicy,
			TokenLogonSid,
			TokenIsAppContainer,
			TokenCapabilities,
			TokenAppContainerSid,
			TokenAppContainerNumber,
			TokenUserClaimAttributes,
			TokenDeviceClaimAttributes,
			TokenRestrictedUserClaimAttributes,
			TokenRestrictedDeviceClaimAttributes,
			TokenDeviceGroups,
			TokenRestrictedDeviceGroups,
			TokenSecurityAttributes,
			TokenIsRestricted,
			TokenProcessTrustLevel,
			TokenPrivateNameSpace,
			TokenSingletonAttributes,
			TokenBnoIsolation,
			TokenChildProcessFlags,
			TokenIsLessPrivilegedAppContainer,
			TokenIsSandboxed
		}

		// ============================================
		// HELPER METHODS
		// ============================================

		/// <summary>
		/// Prüft ob Administrator-Rechte vorhanden sind
		/// </summary>
		public static bool IsRunningAsAdministrator()
		{
			try
			{
				using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
				{
					WindowsPrincipal principal = new WindowsPrincipal(identity);
					return principal.IsInRole(WindowsBuiltInRole.Administrator);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to check administrator privileges", ex);
				return false;
			}
		}

		/// <summary>
		/// Holt die aktuelle Prozess-ID
		/// </summary>
		public static int GetCurrentProcessId()
		{
			using (Process process = Process.GetCurrentProcess())
			{
				return process.Id;
			}
		}

		/// <summary>
		/// Holt die Thread-ID des aktuellen Threads
		/// </summary>
		public static int GetCurrentThreadId()
		{
			return Environment.CurrentManagedThreadId;
		}

		/// <summary>
		/// Prüft ob Prozess 64-bit ist
		/// </summary>
		public static bool Is64BitProcess()
		{
			return Environment.Is64BitProcess;
		}

		/// <summary>
		/// Prüft ob Betriebssystem 64-bit ist
		/// </summary>
		public static bool Is64BitOperatingSystem()
		{
			return Environment.Is64BitOperatingSystem;
		}

		/// <summary>
		/// Holt Windows Build Number
		/// </summary>
		public static int GetWindowsBuildNumber()
		{
			return Environment.OSVersion.Version.Build;
		}

		/// <summary>
		/// Holt Windows Major Version
		/// </summary>
		public static int GetWindowsMajorVersion()
		{
			return Environment.OSVersion.Version.Major;
		}

		/// <summary>
		/// Prüft ob Windows 10 oder neuer
		/// </summary>
		public static bool IsWindows10OrNewer()
		{
			return GetWindowsMajorVersion() >= 10;
		}

		/// <summary>
		/// Prüft ob Windows 11 oder neuer
		/// </summary>
		public static bool IsWindows11OrNewer()
		{
			// Windows 11 hat Version 10, Build >= 22000
			return GetWindowsMajorVersion() >= 10 && GetWindowsBuildNumber() >= 22000;
		}

		/// <summary>
		/// Holt System-Uptime in Millisekunden
		/// </summary>
		public static ulong GetSystemUptime()
		{
			try
			{
				return GetTickCount64();
			}
			catch (Exception ex)
			{
				_logger.LogWarning("GetTickCount64 failed, using GetTickCount", ex);
				return GetTickCount();
			}
		}

		/// <summary>
		/// Holt High-Resolution Timer Wert
		/// </summary>
		public static long GetHighResolutionTimestamp()
		{
			QueryPerformanceCounter(out long timestamp);
			return timestamp;
		}

		/// <summary>
		/// Holt High-Resolution Timer Frequenz
		/// </summary>
		public static long GetHighResolutionFrequency()
		{
			QueryPerformanceFrequency(out long frequency);
			return frequency;
		}

		/// <summary>
		/// Berechnet verstrichene Zeit in Millisekunden zwischen zwei High-Resolution Timestamps
		/// </summary>
		public static double CalculateElapsedMilliseconds(long startTimestamp, long endTimestamp)
		{
			long frequency = GetHighResolutionFrequency();
			if (frequency == 0) return 0;

			return (endTimestamp - startTimestamp) * 1000.0 / frequency;
		}

		/// <summary>
		/// Holt CPU-Kern Anzahl
		/// </summary>
		public static int GetProcessorCount()
		{
			return Environment.ProcessorCount;
		}

		/// <summary>
		/// Holt logische Prozessor Anzahl (mit Hyper-Threading)
		/// </summary>
		public static int GetLogicalProcessorCount()
		{
			return Environment.ProcessorCount;
		}

		/// <summary>
		/// Holt physische Prozessor Anzahl
		/// </summary>
		public static int GetPhysicalProcessorCount()
		{
			try
			{
				int physicalCores = 0;
				foreach (var item in new System.Management.ManagementObjectSearcher(
					"Select NumberOfCores from Win32_Processor").Get())
				{
					physicalCores += int.Parse(item["NumberOfCores"].ToString());
				}
				return physicalCores;
			}
			catch
			{
				// Fallback: logische Prozessoren / 2 (angenommen Hyper-Threading)
				return GetLogicalProcessorCount() / 2;
			}
		}

		/// <summary>
		/// Prüft ob Hyper-Threading aktiviert ist
		/// </summary>
		public static bool IsHyperThreadingEnabled()
		{
			return GetLogicalProcessorCount() > GetPhysicalProcessorCount();
		}

		/// <summary>
		/// Prüft ob bestimmte CPU-Feature verfügbar ist
		/// </summary>
		public static bool IsProcessorFeatureAvailable(ProcessorFeature feature)
		{
			try
			{
				return IsProcessorFeaturePresent((int)feature);
			}
			catch (Exception ex)
			{
				_logger.LogWarning($"Failed to check processor feature {feature}", ex);
				return false;
			}
		}

		/// <summary>
		/// Holt System-Arbeitsspeicher Information
		/// </summary>
		public static MemoryInfo GetMemoryInfo()
		{
			try
			{
				var memoryInfo = new MemoryInfo();

				// Total Physical Memory
				var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
				memoryInfo.TotalPhysical = computerInfo.TotalPhysicalMemory;
				memoryInfo.AvailablePhysical = computerInfo.AvailablePhysicalMemory;

				// Total Virtual Memory
				memoryInfo.TotalVirtual = computerInfo.TotalVirtualMemory;
				memoryInfo.AvailableVirtual = computerInfo.AvailableVirtualMemory;

				// Page File
				memoryInfo.TotalPageFile = computerInfo.TotalVirtualMemory - computerInfo.TotalPhysicalMemory;
				memoryInfo.AvailablePageFile = computerInfo.AvailableVirtualMemory - computerInfo.AvailablePhysicalMemory;

				return memoryInfo;
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to get memory info", ex);
				return new MemoryInfo();
			}
		}

		/// <summary>
		/// Holt Prozess-Arbeitsspeicher Information
		/// </summary>
		public static ProcessMemoryInfo GetProcessMemoryInfo()
		{
			try
			{
				using (Process process = Process.GetCurrentProcess())
				{
					return new ProcessMemoryInfo
					{
						WorkingSet = process.WorkingSet64,
						PrivateMemory = process.PrivateMemorySize64,
						VirtualMemory = process.VirtualMemorySize64,
						PagedMemory = process.PagedMemorySize64,
						NonPagedMemory = process.NonpagedSystemMemorySize64,
						PagedSystemMemory = process.PagedSystemMemorySize64,
						PeakWorkingSet = process.PeakWorkingSet64,
						PeakVirtualMemory = process.PeakVirtualMemorySize64
					};
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to get process memory info", ex);
				return new ProcessMemoryInfo();
			}
		}

		/// <summary>
		/// Setzt Prozess-Priorität
		/// </summary>
		public static bool SetProcessPriority(ProcessPriority priority)
		{
			try
			{
				using (Process process = Process.GetCurrentProcess())
				{
					process.PriorityClass = priority switch
					{
						ProcessPriority.Idle => ProcessPriorityClass.Idle,
						ProcessPriority.BelowNormal => ProcessPriorityClass.BelowNormal,
						ProcessPriority.Normal => ProcessPriorityClass.Normal,
						ProcessPriority.AboveNormal => ProcessPriorityClass.AboveNormal,
						ProcessPriority.High => ProcessPriorityClass.High,
						ProcessPriority.RealTime => ProcessPriorityClass.RealTime,
						_ => ProcessPriorityClass.Normal
					};

					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failed to set process priority to {priority}", ex);
				return false;
			}
		}

		/// <summary>
		/// Setzt Thread-Priorität
		/// </summary>
		public static bool SetThreadPriority(ThreadPriority priority)
		{
			try
			{
				System.Threading.Thread.CurrentThread.Priority = priority;
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failed to set thread priority to {priority}", ex);
				return false;
			}
		}

		/// <summary>
		/// Setzt Prozess-Affinität (CPU-Kerne Zuweisung)
		/// </summary>
		public static bool SetProcessAffinity(ulong affinityMask)
		{
			try
			{
				using (Process process = Process.GetCurrentProcess())
				{
					process.ProcessorAffinity = new IntPtr((long)affinityMask);
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failed to set process affinity {affinityMask:X}", ex);
				return false;
			}
		}

		/// <summary>
		/// Aktiviert/Deaktiviert Privileg für aktuellen Prozess
		/// </summary>
		public static bool EnablePrivilege(string privilegeName, bool enable)
		{
			try
			{
				IntPtr tokenHandle = IntPtr.Zero;

				try
				{
					// Prozess Token öffnen
					if (!OpenProcessToken(GetCurrentProcess(), 0x00000020, out tokenHandle))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}

					// LUID für Privileg holen
					LUID luid;
					if (!LookupPrivilegeValue(null, privilegeName, out luid))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}

					// TOKEN_PRIVILEGES Struktur erstellen
					TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
					{
						PrivilegeCount = 1,
						Privileges = new LUID_AND_ATTRIBUTES
						{
							Luid = luid,
							Attributes = enable ? 0x00000002U : 0x00000000U // SE_PRIVILEGE_ENABLED
						}
					};

					// Privileg anpassen
					if (!AdjustTokenPrivileges(
						tokenHandle,
						false,
						ref tokenPrivileges,
						0,
						IntPtr.Zero,
						IntPtr.Zero))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}

					return true;
				}
				finally
				{
					if (tokenHandle != IntPtr.Zero)
					{
						CloseHandle(tokenHandle);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failed to {(enable ? "enable" : "disable")} privilege {privilegeName}", ex);
				return false;
			}
		}

		/// <summary>
		/// Holt System-Leistungsinformationen
		/// </summary>
		public static SystemPerformanceInfo GetSystemPerformanceInfo()
		{
			try
			{
				var info = new SystemPerformanceInfo();

				// CPU Auslastung
				GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

				ulong totalTime = kernelTime.ToUInt64() + userTime.ToUInt64();
				info.CpuUsage = totalTime > 0 ?
					(1.0 - (double)idleTime.ToUInt64() / totalTime) * 100.0 : 0;

				// Memory Info
				var memInfo = GetMemoryInfo();
				info.MemoryUsage = (1.0 - (double)memInfo.AvailablePhysical / memInfo.TotalPhysical) * 100.0;

				// Thread Count
				info.ThreadCount = Process.GetCurrentProcess().Threads.Count;

				// Handle Count
				info.HandleCount = Process.GetCurrentProcess().HandleCount;

				// Uptime
				info.SystemUptime = TimeSpan.FromMilliseconds(GetSystemUptime());

				return info;
			}
			catch (Exception ex)
			{
				_logger.LogError("Failed to get system performance info", ex);
				return new SystemPerformanceInfo();
			}
		}

		/// <summary>
		/// Führt einen sicheren Registry-Zugriff durch
		/// </summary>
		public static SafeRegistryResult SafeRegistryAccess(Action<IntPtr> action, IntPtr hKey, string subKey)
		{
			IntPtr keyHandle = IntPtr.Zero;
			uint result = 0;

			try
			{
				// Registry Key öffnen
				result = RegOpenKeyEx(hKey, subKey, 0, 0x20019, out keyHandle);
				if (result != 0)
				{
					throw new Win32Exception((int)result);
				}

				// Aktion ausführen
				action(keyHandle);

				return new SafeRegistryResult { Success = true };
			}
			catch (Exception ex)
			{
				return new SafeRegistryResult
				{
					Success = false,
					ErrorCode = result,
					ErrorMessage = ex.Message
				};
			}
			finally
			{
				if (keyHandle != IntPtr.Zero)
				{
					RegCloseKey(keyHandle);
				}
			}
		}

		/// <summary>
		/// Erstellt einen Memory-Leak sicheren Buffer
		/// </summary>
		public static SafeBuffer CreateSafeBuffer(int size)
		{
			IntPtr heap = GetProcessHeap();
			IntPtr buffer = HeapAlloc(heap, 0x00000008, new UIntPtr((uint)size)); // HEAP_ZERO_MEMORY

			return new SafeBuffer(buffer, size);
		}

		// ============================================
		// ENUMS & DATA CLASSES
		// ============================================

		public enum ProcessorFeature
		{
			PF_FLOATING_POINT_PRECISION_ERRATA = 0,
			PF_FLOATING_POINT_EMULATED = 1,
			PF_COMPARE_EXCHANGE_DOUBLE = 2,
			PF_MMX_INSTRUCTIONS_AVAILABLE = 3,
			PF_PPC_MOVEMEM_64BIT_OK = 4,
			PF_ALPHA_BYTE_INSTRUCTIONS = 5,
			PF_XMMI_INSTRUCTIONS_AVAILABLE = 6,
			PF_3DNOW_INSTRUCTIONS_AVAILABLE = 7,
			PF_RDTSC_INSTRUCTION_AVAILABLE = 8,
			PF_PAE_ENABLED = 9,
			PF_XMMI64_INSTRUCTIONS_AVAILABLE = 10,
			PF_SSE_DAZ_MODE_AVAILABLE = 11,
			PF_NX_ENABLED = 12,
			PF_SSE3_INSTRUCTIONS_AVAILABLE = 13,
			PF_COMPARE_EXCHANGE128 = 14,
			PF_COMPARE64_EXCHANGE128 = 15,
			PF_CHANNELS_ENABLED = 16,
			PF_XSAVE_ENABLED = 17,
			PF_ARM_VFP_32REGISTERS_AVAILABLE = 18,
			PF_ARM_NEON_INSTRUCTIONS_AVAILABLE = 19,
			PF_SECOND_LEVEL_ADDRESS_TRANSLATION = 20,
			PF_VIRT_FIRMWARE_ENABLED = 21,
			PF_RDWRFSGSBASE_AVAILABLE = 22,
			PF_FASTFAIL_AVAILABLE = 23,
			PF_ARM_DIVIDE_INSTRUCTION_AVAILABLE = 24,
			PF_ARM_64BIT_LOADSTORE_ATOMIC = 25,
			PF_ARM_EXTERNAL_CACHE_AVAILABLE = 26,
			PF_ARM_FMAC_INSTRUCTIONS_AVAILABLE = 27,
			PF_RDRAND_INSTRUCTION_AVAILABLE = 28,
			PF_ARM_V8_INSTRUCTIONS_AVAILABLE = 29,
			PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE = 30,
			PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE = 31,
			PF_RDTSCP_INSTRUCTION_AVAILABLE = 32
		}

		public enum ProcessPriority
		{
			Idle,
			BelowNormal,
			Normal,
			AboveNormal,
			High,
			RealTime
		}

		public class MemoryInfo
		{
			public ulong TotalPhysical { get; set; }
			public ulong AvailablePhysical { get; set; }
			public ulong TotalVirtual { get; set; }
			public ulong AvailableVirtual { get; set; }
			public ulong TotalPageFile { get; set; }
			public ulong AvailablePageFile { get; set; }

			public double PhysicalUsagePercent => TotalPhysical > 0 ?
				(1.0 - (double)AvailablePhysical / TotalPhysical) * 100.0 : 0;

			public double VirtualUsagePercent => TotalVirtual > 0 ?
				(1.0 - (double)AvailableVirtual / TotalVirtual) * 100.0 : 0;
		}

		public class ProcessMemoryInfo
		{
			public long WorkingSet { get; set; }          // Arbeitsspeicher in Bytes
			public long PrivateMemory { get; set; }       // Privater Arbeitsspeicher
			public long VirtualMemory { get; set; }       // Virtueller Speicher
			public long PagedMemory { get; set; }         // Ausgelagerter Speicher
			public long NonPagedMemory { get; set; }      // Nicht-ausgelagerter Speicher
			public long PagedSystemMemory { get; set; }   // Ausgelagerter System-Speicher
			public long PeakWorkingSet { get; set; }      // Maximaler Arbeitsspeicher
			public long PeakVirtualMemory { get; set; }   // Maximaler virtueller Speicher

			// Helper Properties
			public double WorkingSetMB => WorkingSet / 1024.0 / 1024.0;
			public double PrivateMemoryMB => PrivateMemory / 1024.0 / 1024.0;
			public double VirtualMemoryMB => VirtualMemory / 1024.0 / 1024.0;
		}

		public class SystemPerformanceInfo
		{
			public double CpuUsage { get; set; }          // CPU Auslastung in %
			public double MemoryUsage { get; set; }       // RAM Auslastung in %
			public int ThreadCount { get; set; }          // Thread Anzahl
			public int HandleCount { get; set; }          // Handle Anzahl
			public TimeSpan SystemUptime { get; set; }    // System Uptime
		}

		public class SafeRegistryResult
		{
			public bool Success { get; set; }
			public uint ErrorCode { get; set; }
			public string ErrorMessage { get; set; }
		}

		/// <summary>
		/// Safe Buffer Wrapper für automatisches Cleanup
		/// </summary>
		public class SafeBuffer : IDisposable
		{
			private readonly IntPtr _buffer;
			private readonly int _size;
			private bool _disposed;

			public IntPtr Pointer => _buffer;
			public int Size => _size;

			public SafeBuffer(IntPtr buffer, int size)
			{
				_buffer = buffer;
				_size = size;
			}

			public byte[] ToArray()
			{
				if (_buffer == IntPtr.Zero)
					return new byte[0];

				byte[] array = new byte[_size];
				Marshal.Copy(_buffer, array, 0, _size);
				return array;
			}

			public void Dispose()
			{
				if (!_disposed && _buffer != IntPtr.Zero)
				{
					IntPtr heap = GetProcessHeap();
					HeapFree(heap, 0, _buffer);
					_disposed = true;
				}
				GC.SuppressFinalize(this);
			}

			~SafeBuffer()
			{
				Dispose();
			}
		}

		// ============================================
		// CONSTANTS
		// ============================================

		public const uint HKEY_LOCAL_MACHINE = 0x80000002;
		public const uint HKEY_CURRENT_USER = 0x80000001;
		public const uint HKEY_CLASSES_ROOT = 0x80000000;
		public const uint HKEY_USERS = 0x80000003;
		public const uint HKEY_CURRENT_CONFIG = 0x80000005;

		public const uint REG_NONE = 0;
		public const uint REG_SZ = 1;
		public const uint REG_EXPAND_SZ = 2;
		public const uint REG_BINARY = 3;
		public const uint REG_DWORD = 4;
		public const uint REG_DWORD_LITTLE_ENDIAN = 4;
		public const uint REG_DWORD_BIG_ENDIAN = 5;
		public const uint REG_LINK = 6;
		public const uint REG_MULTI_SZ = 7;
		public const uint REG_RESOURCE_LIST = 8;
		public const uint REG_FULL_RESOURCE_DESCRIPTOR = 9;
		public const uint REG_RESOURCE_REQUIREMENTS_LIST = 10;
		public const uint REG_QWORD = 11;
		public const uint REG_QWORD_LITTLE_ENDIAN = 11;

		// System Metrics Constants
		public const int SM_CXSCREEN = 0;
		public const int SM_CYSCREEN = 1;
		public const int SM_CXVSCROLL = 2;
		public const int SM_CYHSCROLL = 3;
		public const int SM_CYCAPTION = 4;
		public const int SM_CXBORDER = 5;
		public const int SM_CYBORDER = 6;
		public const int SM_CXDLGFRAME = 7;
		public const int SM_CYDLGFRAME = 8;
		public const int SM_CYVTHUMB = 9;
		public const int SM_CXHTHUMB = 10;
		public const int SM_CXICON = 11;
		public const int SM_CYICON = 12;
		public const int SM_CXCURSOR = 13;
		public const int SM_CYCURSOR = 14;
		public const int SM_CYMENU = 15;
		public const int SM_CXFULLSCREEN = 16;
		public const int SM_CYFULLSCREEN = 17;
		public const int SM_CYKANJIWINDOW = 18;
		public const int SM_MOUSEPRESENT = 19;
		public const int SM_CYVSCROLL = 20;
		public const int SM_CXHSCROLL = 21;
		public const int SM_DEBUG = 22;
		public const int SM_SWAPBUTTON = 23;
		public const int SM_RESERVED1 = 24;
		public const int SM_RESERVED2 = 25;
		public const int SM_RESERVED3 = 26;
		public const int SM_RESERVED4 = 27;
		public const int SM_CXMIN = 28;
		public const int SM_CYMIN = 29;
		public const int SM_CXSIZE = 30;
		public const int SM_CYSIZE = 31;
		public const int SM_CXFRAME = 32;
		public const int SM_CYFRAME = 33;
		public const int SM_CXMINTRACK = 34;
		public const int SM_CYMINTRACK = 35;
		public const int SM_CXDOUBLECLK = 36;
		public const int SM_CYDOUBLECLK = 37;
		public const int SM_CXICONSPACING = 38;
		public const int SM_CYICONSPACING = 39;
		public const int SM_MENUDROPALIGNMENT = 40;
		public const int SM_PENWINDOWS = 41;
		public const int SM_DBCSENABLED = 42;
		public const int SM_CMOUSEBUTTONS = 43;
		public const int SM_SECURE = 44;
		public const int SM_CXEDGE = 45;
		public const int SM_CYEDGE = 46;
		public const int SM_CXMINSPACING = 47;
		public const int SM_CYMINSPACING = 48;
		public const int SM_CXSMICON = 49;
		public const int SM_CYSMICON = 50;
		public const int SM_CYSMCAPTION = 51;
		public const int SM_CXSMSIZE = 52;
		public const int SM_CYSMSIZE = 53;
		public const int SM_CXMENUSIZE = 54;
		public const int SM_CYMENUSIZE = 55;
		public const int SM_ARRANGE = 56;
		public const int SM_CXMINIMIZED = 57;
		public const int SM_CYMINIMIZED = 58;
		public const int SM_CXMAXTRACK = 59;
		public const int SM_CYMAXTRACK = 60;
		public const int SM_CXMAXIMIZED = 61;
		public const int SM_CYMAXIMIZED = 62;
		public const int SM_NETWORK = 63;
		public const int SM_CLEANBOOT = 67;
		public const int SM_CXDRAG = 68;
		public const int SM_CYDRAG = 69;
		public const int SM_SHOWSOUNDS = 70;
		public const int SM_CXMENUCHECK = 71;
		public const int SM_CYMENUCHECK = 72;
		public const int SM_SLOWMACHINE = 73;
		public const int SM_MIDEASTENABLED = 74;
		public const int SM_MOUSEWHEELPRESENT = 75;
		public const int SM_XVIRTUALSCREEN = 76;
		public const int SM_YVIRTUALSCREEN = 77;
		public const int SM_CXVIRTUALSCREEN = 78;
		public const int SM_CYVIRTUALSCREEN = 79;
		public const int SM_CMONITORS = 80;
		public const int SM_SAMEDISPLAYFORMAT = 81;
		public const int SM_IMMENABLED = 82;
		public const int SM_CXFOCUSBORDER = 83;
		public const int SM_CYFOCUSBORDER = 84;
		public const int SM_TABLETPC = 86;
		public const int SM_MEDIACENTER = 87;
		public const int SM_STARTER = 88;
		public const int SM_SERVERR2 = 89;
		public const int SM_MOUSEHORIZONTALWHEELPRESENT = 91;
		public const int SM_CXPADDEDBORDER = 92;
		public const int SM_DIGITIZER = 94;
		public const int SM_MAXIMUMTOUCHES = 95;
		public const int SM_REMOTESESSION = 0x1000;
		public const int SM_SHUTTINGDOWN = 0x2000;
		public const int SM_REMOTECONTROL = 0x2001;
		public const int SM_CARETBLINKINGENABLED = 0x2002;
	}
}