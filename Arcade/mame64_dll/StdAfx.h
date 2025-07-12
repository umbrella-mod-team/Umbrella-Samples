// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//

#if !defined(AFX_STDAFX_H__64AACD08_A1F6_459E_B8C3_0BA06AFF0E20__INCLUDED_)
#define AFX_STDAFX_H__64AACD08_A1F6_459E_B8C3_0BA06AFF0E20__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000


// Insert your headers here
#define WIN32_LEAN_AND_MEAN		// Exclude rarely-used stuff from Windows headers

#pragma pack(push)
#pragma pack(8)
#include <windows.h>
#include <tchar.h>
#include <wtypes.h>
#include <OLEAUTO.H>
#pragma pack(pop)

//#define DEBUG_OUTPUT

#ifdef DEBUG_OUTPUT
	#define DEBUGLOG(id, str, ...) Debug(id, str, __VA_ARGS__);
#else
    #define DEBUGLOG(id, str, ...)
#endif

VOID Debug(LPCTSTR szId, LPCTSTR szFormat, ...);
BSTR T2W(LPCSTR s);

// TODO: reference additional headers your program requires here

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_STDAFX_H__64AACD08_A1F6_459E_B8C3_0BA06AFF0E20__INCLUDED_)
