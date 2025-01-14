
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Types;
using System.Globalization;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	/// <summary>
	/// Control to accept a linedef action argument.
	/// </summary>
	public partial class ArgumentBox : UserControl
	{
		#region ================== Variables

		private TypeHandler typehandler;
		private bool ignorebuttonchange = false;
		
		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor

		// Constructor
		public ArgumentBox()
		{
			// Initialize
			InitializeComponent();
			scrollbuttons.Value = 0;
			combobox.MouseWheel += combobox_MouseWheel;
		}

		#endregion
		
		#region ================== Events

		// When control resizes
		private void ArgumentBox_Resize(object sender, EventArgs e)
		{
			if(button.Visible)
				combobox.Width = ClientRectangle.Width - button.Width - 2;
			else if(scrollbuttons.Visible)
				combobox.Width = ClientRectangle.Width - scrollbuttons.Width - 2;
			else
				combobox.Width = ClientRectangle.Width;

			button.Left = ClientRectangle.Width - button.Width;
			scrollbuttons.Left = ClientRectangle.Width - scrollbuttons.Width;
			Height = button.Height;
		}

		// When control layout is aplied
		private void ArgumentBox_Layout(object sender, LayoutEventArgs e)
		{
			ArgumentBox_Resize(sender, e);
		}

		// When the entered value needs to be validated
		private void combobox_Validating(object sender, CancelEventArgs e)
		{
			string str = combobox.Text.Trim().ToLowerInvariant();
			str = str.TrimStart('+', '-');
			int num;
			
			// Anything in the box?
			if(combobox.Text.Trim().Length > 0)
			{
				// Prefixed?
				if(CheckIsRelative())
				{
					// Try parsing to number
					if(!int.TryParse(str, NumberStyles.Integer, CultureInfo.CurrentCulture, out num))
					{
						// Invalid relative number
						combobox.SelectedItem = null;
						combobox.Text = "";
					}
				}
				else
				{
					// Set the value. The type handler will validate it
					// and make the best possible choice.
					typehandler.SetValue(combobox.Text);
					combobox.SelectedItem = null;
					combobox.Text = typehandler.GetStringValue();
				}
			}
		}

		// When browse button is clicked
		private void button_Click(object sender, EventArgs e)
		{
			// Browse for a value
			typehandler.Browse(this);
			combobox.SelectedItem = null;
			combobox.Text = typehandler.GetStringValue();
			combobox.Focus();
			combobox.SelectAll();
		}

		// Text changes
		private void combobox_TextChanged(object sender, EventArgs e)
		{
			scrollbuttons.Enabled = !CheckIsRelative();
		}

		// Mouse wheel used
		private void combobox_MouseWheel(object sender, MouseEventArgs e)
		{
			if(scrollbuttons.Visible)
			{
				if(e.Delta < 0)
					scrollbuttons.Value += 1;
				else if(e.Delta > 0)
					scrollbuttons.Value -= 1;
			}
		}
		
		// Scroll buttons clicked
		private void scrollbuttons_ValueChanged(object sender, EventArgs e)
		{
			if(!ignorebuttonchange)
			{
				ignorebuttonchange = true;
				if(!CheckIsRelative())
				{
					typehandler.SetValue(combobox.Text);
					int newvalue = GetResult(0) - scrollbuttons.Value;
					if(newvalue < 0) newvalue = 0;
					combobox.Text = newvalue.ToString();
					combobox_Validating(sender, new CancelEventArgs());
				}
				scrollbuttons.Value = 0;
				ignorebuttonchange = false;
			}
		}
		
		#endregion
		
		#region ================== Methods
		
		// This sets up the control for a specific argument
		public void Setup(ArgumentInfo arginfo)
		{
			int oldvalue = 0;
			
			// Get the original value
			if(typehandler != null) oldvalue = typehandler.GetIntValue();
			
			// Get the type handler
			typehandler = General.Types.GetArgumentHandler(arginfo);
			
			// Clear combobox
			combobox.SelectedItem = null;
			combobox.Items.Clear();

			// Check if this supports enumerated options
			if(typehandler.IsEnumerable)
			{
				// Show the combobox
				button.Visible = false;
				scrollbuttons.Visible = false;
				combobox.DropDownStyle = ComboBoxStyle.DropDown;
				combobox.Items.AddRange(typehandler.GetEnumList().ToArray());
			}
			// Check if browsable
			else if(typehandler.IsBrowseable)
			{
				// Show the button
				button.Visible = true;
				scrollbuttons.Visible = false;
				combobox.DropDownStyle = ComboBoxStyle.Simple;
			}
			else
			{
				// Show textbox with scroll buttons
				button.Visible = false;
				scrollbuttons.Visible = true;
				combobox.DropDownStyle = ComboBoxStyle.Simple;
			}
			
			// Setup layout
			ArgumentBox_Resize(this, EventArgs.Empty);
			
			// Re-apply value
			SetValue(oldvalue);
		}

		// This sets the value
		public void SetValue(int value)
		{
			typehandler.SetValue(value);
			combobox.SelectedItem = null;
			combobox.Text = typehandler.GetStringValue();
			combobox_Validating(this, new CancelEventArgs());
		}

		// This clears the value
		public void ClearValue()
		{
			typehandler.SetValue("");
			combobox.SelectedItem = null;
			combobox.Text = "";
		}

		// This checks if the number is relative
		public bool CheckIsRelative()
		{
			// Prefixed with ++ or --?
			return (combobox.Text.Trim().StartsWith("++") || combobox.Text.Trim().StartsWith("--"));
		}
		
		// This returns the selected value
		public int GetResult(int original)
		{
			int result = 0;
			
			// Strip prefixes
			string str = combobox.Text.Trim().ToLowerInvariant();
			str = str.TrimStart('+', '-');
			int num = original;

			// Anything in the box?
			if(combobox.Text.Trim().Length > 0)
			{
				// Prefixed with ++?
				if(combobox.Text.Trim().StartsWith("++"))
				{
					// Add number to original
					if(!int.TryParse(str, out num)) num = 0;
					result = original + num;
				}
				// Prefixed with --?
				else if(combobox.Text.Trim().StartsWith("--"))
				{
					// Subtract number from original
					if(!int.TryParse(str, out num)) num = 0;
					result = original - num;
				}
				else
				{
					// Return the value
					result = typehandler.GetIntValue();
				}
			}
			else
			{
				// Just return the original
				result = original;
			}

			return General.Clamp(result, General.Map.FormatInterface.MinArgument, General.Map.FormatInterface.MaxArgument);
		}
		
		#endregion
	}
}
