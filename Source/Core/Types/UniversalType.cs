
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

#endregion

namespace CodeImp.DoomBuilder.Types
{
	public enum UniversalType : int
	{
		Integer = 0,
		Float = 1,
		String = 2,
		Boolean = 3,
		LinedefType = 4,
		SectorEffect = 5,
		Texture = 6,
		Flat = 7,
		AngleDegrees = 8,
		AngleRadians = 9,
		Color = 10,
		EnumOption = 11,
		EnumBits = 12,
		SectorTag = 13,
		ThingTag = 14,
		LinedefTag = 15,
		EnumStrings = 16,
		AngleDegreesFloat = 17
	}
}
