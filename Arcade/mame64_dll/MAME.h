
// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the MAME_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// MAME_API functions as being imported from a DLL, wheras this DLL sees symbols
// defined with this macro as being exported.
#ifdef MAME_EXPORTS
#define MAME_API __declspec(dllexport)
#else
#define MAME_API __declspec(dllimport)
#endif

#ifndef MAME_H
#define MAME_H

#include <windows.h>
#include <map>
#include <string>

// Function prototypes
extern "C" {
    MAME_API LRESULT handle_mame_start(WPARAM wparam, LPARAM lparam);
    MAME_API LRESULT handle_mame_stop(WPARAM wparam, LPARAM lparam);
    MAME_API LRESULT handle_copydata(WPARAM wparam, LPARAM lparam);
    MAME_API LRESULT handle_update_state(WPARAM wparam, LPARAM lparam);

    void reset_id_to_outname_cache();
    const char* map_id_to_outname(WPARAM id);
}

#endif // MAME_H
