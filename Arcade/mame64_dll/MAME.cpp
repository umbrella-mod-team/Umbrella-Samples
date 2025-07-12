// MAME.cpp : Defines the entry point for the DLL application.
//
#include "stdafx.h"
#include "MAME.h"

// standard windows headers
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <conio.h>
#include <winioctl.h>
#include <winsock2.h>
#include <errno.h>
 
// standard C headers
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <wtypes.h>
#include <OLEAUTO.H>

#include <comutil.h>

// MAME output header file
typedef void running_machine;
#include "output.h"

#pragma comment(lib,"ws2_32.lib")		// Winsock Library

//============================================================
//  CONSTANTS
//============================================================

// window styles
#define WINDOW_STYLE						WS_OVERLAPPEDWINDOW
#define WINDOW_STYLE_EX						0

#define SERVER_IP							"127.0.0.1"		// IP address of tcp server
#define SERVER_PORT							8000			// The port on which to listen for incoming data
#define BUFFER_LENGTH						512				// Max length of buffer
#define SOCKET_READ_TIMEOUT_MILLISECONDS	500				// Read timeout in seconds

#define WM_MAME_MESSAGE						(WM_APP + 1)

// Uncomment this to compile for VB6 string support
//#define COMPILE_FOR_VB6_STRINGS

//============================================================
//  TYPE DEFINITIONS
//============================================================

typedef struct _id_map_entry id_map_entry;
struct _id_map_entry
{
	id_map_entry *			next;
	const char *			name;
	WPARAM					id;
};

typedef struct tagWNDINFO {
	DWORD   dwProcessID;
	HWND    hWnd;
} WNDINFO;

#ifdef COMPILE_FOR_VB6_STRINGS
typedef int (__stdcall *MAME_START)(int hWnd);
typedef int (__stdcall *MAME_STOP)(void);
typedef int (__stdcall *MAME_COPYDATA)(int id, const BSTR name);
typedef int (__stdcall *MAME_OUTPUT)(BSTR name, int value);
#else
typedef int (__stdcall *MAME_START)(int hWnd);
typedef int (__stdcall *MAME_STOP)(void);
typedef int (__stdcall *MAME_COPYDATA)(int id, const char *name);
typedef int (__stdcall *MAME_OUTPUT)(const char *name, int value);
#endif

//============================================================
//  GLOBAL VARIABLES
//============================================================

HINSTANCE				instance = NULL;

HANDLE					window_thread = NULL;
HANDLE					winsock_thread = NULL;

DWORD					window_thread_id = 0;
DWORD					winsock_thread_id = 0;

int						tcp_socket = 0;

WCHAR					window_class[256] = { 0 };
WCHAR					window_name[256] = { 0 };

int						client_id = 0;

HWND					mame_target = NULL;
HWND					listener_hwnd = NULL;

MAME_START				mame_start = NULL;
MAME_STOP				mame_stop = NULL;
MAME_COPYDATA			mame_copydata = NULL;
MAME_OUTPUT				mame_output = NULL;

id_map_entry *			idmaplist = NULL;

// output message IDs
UINT					om_mame_start = 0;
UINT					om_mame_stop = 0;
UINT					om_mame_update_state = 0;
UINT					om_mame_register_client = 0;
UINT					om_mame_unregister_client = 0;
UINT					om_mame_get_id_string = 0;

// input message IDs
UINT					im_mame_message = 0;

//============================================================
//  FUNCTION PROTOTYPES
//============================================================

MAME_API int __stdcall init_mame(int clientid, PWCHAR name, MAME_START start, MAME_STOP stop, MAME_COPYDATA copydata, MAME_OUTPUT output, bool useNetworkOutput);
MAME_API int __stdcall close_mame(void);
MAME_API int __stdcall message_mame(WPARAM id, LPARAM value);

int create_window_class(void);
LRESULT CALLBACK listener_window_proc(HWND wnd, UINT message, WPARAM wparam, LPARAM lparam);
LRESULT handle_mame_start(WPARAM wparam, LPARAM lparam);
LRESULT handle_mame_stop(WPARAM wparam, LPARAM lparam);
LRESULT handle_copydata(WPARAM wparam, LPARAM lparam);
void reset_id_to_outname_cache(void);
static const char *map_id_to_outname(WPARAM id);
LRESULT handle_update_state(WPARAM wparam, LPARAM lparam);

//============================================================
//  main
//============================================================

BOOL APIENTRY DllMain(HANDLE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
		case DLL_PROCESS_ATTACH:
			instance = (HINSTANCE) hModule;
			DisableThreadLibraryCalls(instance);
			break;
		case DLL_PROCESS_DETACH:
			SendMessage(listener_hwnd, WM_CLOSE, 0, 0);
			break;
	}
	return TRUE;
}

DWORD WINAPI create_listener_window(LPVOID lpParam)
{
	int exitcode = 1;
	WNDCLASS wc = { 0 };

	// initialize the description of the window class
	wc.lpszClassName 	= window_class;
	wc.hInstance 		= instance;
	wc.lpfnWndProc		= listener_window_proc;

	// register the class; fail if we can't
	if (!RegisterClass(&wc))
	{
		DEBUGLOG(L"MAMEINTEROP", L"Failed to Register Class %S", window_class);

		goto error;
	}

	// create a window
	listener_hwnd = CreateWindowEx(
						WINDOW_STYLE_EX,
						window_class,
						window_name,
						WINDOW_STYLE,
						0, 0,
						1, 1,
						NULL,
						NULL,
						instance,
						NULL);

	if (listener_hwnd == NULL)
	{
		DEBUGLOG(L"MAMEINTEROP", L"Failed to Create Window %S", window_name);

		goto error;
	}

	// allocate message ids
	if((om_mame_start = RegisterWindowMessage(OM_MAME_START)) == 0)
		goto error;

	if((om_mame_stop = RegisterWindowMessage(OM_MAME_STOP)) == 0)
		goto error;

	if((om_mame_update_state = RegisterWindowMessage(OM_MAME_UPDATE_STATE)) == 0)
		goto error;

	if((om_mame_register_client = RegisterWindowMessage(OM_MAME_REGISTER_CLIENT)) == 0)
		goto error;

	if((om_mame_unregister_client = RegisterWindowMessage(OM_MAME_UNREGISTER_CLIENT)) == 0)
		goto error;

	if((om_mame_get_id_string = RegisterWindowMessage(OM_MAME_GET_ID_STRING)) == 0)
		goto error;

	if ((im_mame_message = RegisterWindowMessage(IM_MAME_MESSAGE)) == 0)
		goto error;

	MSG msg;

	while(GetMessage(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	exitcode = msg.wParam;

error:

	UnregisterClass(window_class, instance);

	return exitcode;
}

//============================================================
//  winsock
//============================================================

int initialize_winsock()
{
	int result = 0;
	WSADATA wsa;

	DEBUGLOG(L"MAMEINTEROP", L"Initializing Winsock...");

	if (WSAStartup(MAKEWORD(2, 2),&wsa) != 0)
	{
		result = WSAGetLastError();

		DEBUGLOG(L"MAMEINTEROP", L"initialize_winsock() failed. Error Code : %d", result);

		goto error;
	}

	return 1;

error:

	return result;
}

int connect_socket()
{
	int result = 0;

	// Create socket
	if ((tcp_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == SOCKET_ERROR)
	{
		result = WSAGetLastError();
		DEBUGLOG(L"MAMEINTEROP", L"socket() failed with error code : %d" , result);

		goto error;
	}

	int dwTimeout = SOCKET_READ_TIMEOUT_MILLISECONDS;
	result = setsockopt(tcp_socket, SOL_SOCKET, SO_RCVTIMEO, (const char *)&dwTimeout, sizeof(dwTimeout));

	// Setup address structure
	sockaddr_in sockAddr;
	sockAddr.sin_family = AF_INET;
	sockAddr.sin_addr.s_addr = inet_addr(SERVER_IP);
	sockAddr.sin_port = htons(SERVER_PORT);

	if(connect(tcp_socket, (SOCKADDR *) &sockAddr, sizeof(sockAddr)) == SOCKET_ERROR)
	{
		result = WSAGetLastError();
		DEBUGLOG(L"MAMEINTEROP", L"connect() failed with error code : %d" , result);

		goto error;
	}

	return 1;

error:

	return result;
}

DWORD WINAPI create_listener_winsock(LPVOID lpParam)
{
	int result = 0;
	char buf[BUFFER_LENGTH];
 
	initialize_winsock();
	 
	connect_socket();

	while(1)
	{
		MSG msg = { 0 };

		while(PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		{
			switch (msg.message)
			{
			case WM_QUIT:
				goto error;
			case WM_MAME_MESSAGE:
				{
					sprintf(buf, "mame_message = %d,%d", msg.wParam, msg.lParam);
					//OutputDebugStringA(buf);

					int send_size = send(tcp_socket, buf, strlen(buf), 0);
					break;
				}
			default:
				TranslateMessage(&msg);
				DispatchMessage(&msg);
				break;
			}
		}

		memset(buf, '\0', BUFFER_LENGTH);

		int recv_size = recv(tcp_socket, buf, BUFFER_LENGTH-1, 0);

		if(recv_size == SOCKET_ERROR)
		{
			result = WSAGetLastError();

			//DEBUG_PRINTF(L"recv() failed with error code : %d", result);

			if(result != WSAEWOULDBLOCK && result != WSAETIMEDOUT)
			{
				closesocket(tcp_socket);

				connect_socket();
			}
		}
		else
		{
			if(recv_size != 0)
			{
				buf[recv_size] = NULL;

				//const char *equals_delimiter = " = ";
				//char *msg_name = strtok(buf, equals_delimiter);
				//char *msg_value = strtok(NULL, equals_delimiter);

				const char* delim = " = ";
				char* pos = strstr(buf, delim);
				*pos = '\0';                                        
				char* msg_name = buf;                            
				char* msg_value = pos + strlen(delim);              
				int state = atoi(msg_value);
				mame_output(msg_name, state);


				//sprintf(buf, "winsock: msg_name [%s] msg_value [%s]\n", msg_name, msg_value);
				//OutputDebugStringA(buf);

				if (strcmp(msg_name, "hello") == 0)
				{
					mame_start(0);

					int send_size = send(tcp_socket, "send_id = 0\r", 11, 0);
				}
				else if (strcmp(msg_name, "mamerun") == 0)
				{
					mame_stop();
				}
				else if (strcmp(msg_name, "req_id") == 0)
				{
					int value_len = strlen(msg_value);

					if (msg_value[value_len - 1] == '\r' || msg_value[value_len - 1] == '\1')
						msg_value[value_len - 1] = NULL;

#ifdef COMPILE_FOR_VB6_STRINGS
					mame_copydata(0, SysAllocString(T2W(msg_value)));
#else
					mame_copydata(0, msg_value);
#endif
				}
				else if (strcmp(msg_name, "mame_start") == 0)
				{
					int value_len = strlen(msg_value);

					if (msg_value[value_len - 1] == '\r' || msg_value[value_len - 1] == '\1')
						msg_value[value_len - 1] = NULL;

#ifdef COMPILE_FOR_VB6_STRINGS
					mame_copydata(0, SysAllocString(T2W(msg_value)));
#else
					mame_copydata(0, msg_value);
#endif
				}
				else if (strcmp(msg_name, "mame_stop") == 0)
				{
					mame_stop();
				}
				else
				{
					int state = atoi(msg_value);

#ifdef COMPILE_FOR_VB6_STRINGS
					mame_output(SysAllocString(T2W(msg_name)), state);
#else
					mame_output(msg_name, state);
#endif
				}
			}
			else
			{
				closesocket(tcp_socket);

				connect_socket();
			}
		}
	}

error:
 
	//OutputDebugString(L"Done.");
	closesocket(tcp_socket);
	WSACleanup();
 
	return 0;
}

MAME_API int __stdcall init_mame(int clientid, PWCHAR name, MAME_START start, MAME_STOP stop, MAME_COPYDATA copydata, MAME_OUTPUT output, bool useNetworkOutput)
{
	HWND otherwnd;

	client_id = clientid;
	mame_start = start;
	mame_stop = stop;
	mame_copydata = copydata;
	mame_output = output;

	wcscpy(window_class, name);
	wcscpy(window_name, name);

	// see if there is another instance of us running
	otherwnd = FindWindow(window_class, window_name);

	// if we had another instance, defer to it
	if (otherwnd != NULL)
		return 0;

	window_thread = CreateThread(NULL, 0, create_listener_window, 0, 0, &window_thread_id);

	if (window_thread == NULL) 
		return 0;

	if (useNetworkOutput)
	{
		winsock_thread = CreateThread(NULL, 0, create_listener_winsock, 0, 0, &winsock_thread_id);

		if (winsock_thread == NULL) 
			return 0;
	}

	// see if MAME is already running
	otherwnd = FindWindow(OUTPUT_WINDOW_CLASS, OUTPUT_WINDOW_NAME);

	if (otherwnd != NULL)
		handle_mame_start((WPARAM)otherwnd, 0);

	return 1;
}

MAME_API int __stdcall close_mame(void)
{
	SendMessage(listener_hwnd, WM_CLOSE, 0, 0);
	PostThreadMessage(winsock_thread_id, WM_QUIT, 0, 0);

	return 1;
}

MAME_API int __stdcall message_mame(WPARAM id, LPARAM value)
{
	PostMessage(mame_target, im_mame_message, id, value);
	PostThreadMessage(winsock_thread_id, WM_MAME_MESSAGE, id, value);

	return 1;
}

//============================================================
//  window_proc
//============================================================

LRESULT CALLBACK listener_window_proc(HWND wnd, UINT message, WPARAM wparam, LPARAM lparam)
{
	// OM_MAME_START: register ourselves with the new MAME (first instance only)
	if (message == om_mame_start)
		return handle_mame_start(wparam, lparam);

	// OM_MAME_STOP: no need to unregister, just note that we've stopped caring and reset the LEDs
	else if (message == om_mame_stop)
		return handle_mame_stop(wparam, lparam);

	// OM_MAME_UPDATE_STATE: update the state of this item if we care
	else if (message == om_mame_update_state)
		return handle_update_state(wparam, lparam);

	// WM_COPYDATA: extract the string and create an ID map entry
	else if (message == WM_COPYDATA)
		return handle_copydata(wparam, lparam);

	switch(message)
	{
	case WM_CLOSE:
		DestroyWindow(wnd);
		break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(wnd, message, wparam, lparam);
	}

	return 0;	
}


//============================================================
//  handle_MAME_start
//============================================================

LRESULT handle_mame_start(WPARAM wparam, LPARAM lparam)
{
	//DEBUGLOG(L"MAMEINTEROP", L"MAME_start (%08X)\n", (UINT32)wparam);

	mame_start((int)wparam);

	// make this the targeted version of MAME
	mame_target = (HWND)wparam;

	reset_id_to_outname_cache();

	// register ourselves as a client
	PostMessage(mame_target, om_mame_register_client, (WPARAM)listener_hwnd, client_id);

	// get the game name
	map_id_to_outname(0);

	return 0;
}


//============================================================
//  handle_MAME_stop
//============================================================

LRESULT handle_mame_stop(WPARAM wparam, LPARAM lparam)
{
	//DEBUG_PRINTF(("MAME_stop (%08X)\n", (UINT32)wparam));

	mame_stop();

	// ignore if this is not the instance we care about
	if (mame_target != (HWND)wparam)
		return 1;

	// clear our target out
	mame_target = NULL;
	reset_id_to_outname_cache();

	return 0;
}


//============================================================
//  handle_copydata
//============================================================

LRESULT handle_copydata(WPARAM wparam, LPARAM lparam)
{
	COPYDATASTRUCT *copydata = (COPYDATASTRUCT *)lparam;
	copydata_id_string *data = (copydata_id_string *)copydata->lpData;
	id_map_entry *entry;
	char *string;

	//DEBUG_PRINTF(("wparam (%08X) lparam (%08X)\n", (UINT32)wparam, (UINT32)lparam));

	//DEBUG_PRINTF(("copydata.dwData (%08X) copydata.cbData (%08X) copydata.lpData (%08X)\n", copydata->dwData, copydata->cbData, copydata->lpData));

	//DEBUG_PRINTF(("sizeof(*data) (%08X) strlen(data->string) (%08X) sizeof(*data) + strlen(data->string) (%08X)\n", sizeof(*data), strlen(data->string), sizeof(*data) + strlen(data->string)));

	// ignore requests we don't care about
	if (mame_target != (HWND)wparam)
		return 1;

	// allocate memory
	entry = (id_map_entry *) malloc(sizeof(id_map_entry));
	string = (char *) malloc(strlen(data->string) + 1);

	// if all allocations worked, make a new entry
	if (entry != NULL && string != NULL)
	{
		entry->next = idmaplist;
		entry->name = string;
		entry->id = data->id;

		// copy the string and hook us into the list
		strcpy(string, data->string);
		idmaplist = entry;

		//DEBUG_PRINTF(("  id %d = '%s'\n", entry->id, entry->name));

#ifdef COMPILE_FOR_VB6_STRINGS
		mame_copydata(data->id, SysAllocString(T2W(entry->name)));
#else
		mame_copydata(data->id, entry->name);
#endif
	}

	return 0;
}


//============================================================
//  reset_id_to_outname_cache
//============================================================

void reset_id_to_outname_cache(void)
{
	// free our ID list
	while (idmaplist != NULL)
	{
		id_map_entry *temp = idmaplist;
		idmaplist = temp->next;
		free((void*)temp->name);
		free(temp);
	}
}


//============================================================
//  map_id_to_outname
//============================================================

static const char *map_id_to_outname(WPARAM id)
{
	id_map_entry *entry;

	// see if we have an entry in our map
	for (entry = idmaplist; entry != NULL; entry = entry->next)
		if (entry->id == id)
			return entry->name;

	// no entry yet; we have to ask
	SendMessage(mame_target, om_mame_get_id_string, (WPARAM)listener_hwnd, id);

	// now see if we have the entry in our map
	for (entry = idmaplist; entry != NULL; entry = entry->next)
		if (entry->id == id)
			return entry->name;

	// if not, use an empty string
	return "";
}


//============================================================
//  handle_update_state
//============================================================

LRESULT handle_update_state(WPARAM wparam, LPARAM lparam)
{
	//DEBUG_PRINTF(("update_state: id=%d state=%d\n", (UINT32)wparam, (UINT32)lparam));

	const char *name = map_id_to_outname(wparam);

#ifdef COMPILE_FOR_VB6_STRINGS
	mame_output(SysAllocString(T2W(name)), lparam);
#else
	mame_output(name, lparam);
#endif

	return 0;
}

BSTR T2W(LPCSTR s)
{
	OLECHAR* oleChar = NULL;
	oleChar = (OLECHAR*)calloc(strlen(s)+1, sizeof(OLECHAR));
	MultiByteToWideChar(CP_ACP, MB_PRECOMPOSED, s, -1, oleChar, strlen(s)+1);  
	BSTR bstr = SysAllocString(oleChar);
	free(oleChar);
	oleChar = NULL;

	return bstr;
}
