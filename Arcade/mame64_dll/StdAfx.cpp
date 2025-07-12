// stdafx.cpp : source file that includes just the standard includes
//	MAME.pch will be the pre-compiled header
//	stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

VOID Debug(LPCTSTR szId, LPCTSTR szFormat, ...)
{
	TCHAR szBuffer[1024];

	wsprintf(szBuffer, L"[%05d:%ls] ", GetCurrentThreadId(), szId);

	va_list pArgs;
	va_start(pArgs, szFormat);
	wvsprintf(szBuffer + lstrlen(szBuffer), szFormat, pArgs);
	va_end(pArgs);

	_tcscat_s(szBuffer, L"\r\n");

	OutputDebugString(szBuffer);
}
