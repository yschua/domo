
/*
DebugUtils.h - Simple debugging utilities.
Copyright (C) 2011 Fabio Varesano <fabio at varesano dot net>

Ideas taken from:
http://www.arduino.cc/cgi-bin/yabb2/YaBB.pl?num=1271517197
https://playground.arduino.cc/Main/Printf

This program is free software: you can redistribute it and/or modify
it under the terms of the version 3 GNU General Public License as
published by the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

#ifndef DEBUGUTILS_H
#define DEBUGUTILS_H

#include <stdarg.h>
#include <string.h>

void p(char *fmt, ...){
  char buf[128];
  va_list args;
  va_start (args, fmt );
  vsnprintf(buf, 128, fmt, args);
  va_end (args);
  Serial.print(buf);
}

#define __FILENAME__ (strrchr(__FILE__, '\\') ? strrchr(__FILE__, '\\') + 1 : __FILE__)

#ifdef DEBUG
#define DEBUG_PRINT(...)              \
  Serial.print(millis());             \
  Serial.print(": ");                 \
  Serial.print(__PRETTY_FUNCTION__);  \
  Serial.print(' ');                  \
  Serial.print(__FILENAME__);         \
  Serial.print(':');                  \
  Serial.print(__LINE__);             \
  Serial.print(' ');                  \
  p(__VA_ARGS__);
#else
#define DEBUG_PRINT(...)
#endif

#endif
