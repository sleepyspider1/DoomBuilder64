
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
using System.Diagnostics;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Rendering;
using SlimDX.Direct3D9;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using CodeImp.DoomBuilder.Map;
using System.Globalization;

#endregion

namespace CodeImp.DoomBuilder.Controls
{
	/// <summary>
	/// Control to view and/or change custom UDMF fields.
	/// </summary>
	public partial class FieldsEditorControl : UserControl
	{
		#region ================== Constants

		// Constants
		private const string ADD_FIELD_TEXT = "   (click to add custom field)";
		private const string FIELD_PREFIX_SUGGESTION = "user_";

		#endregion
		
		#region ================== Variables

		// Variables
		private string elementname;
		private string lasteditfieldname;

		#endregion

		#region ================== Constructor

		// Constructor
		public FieldsEditorControl()
		{
			InitializeComponent();
			enumscombo.Location = new Point(-1000, 1);
		}

		#endregion

		#region ================== Setup / Apply

		// This sets up the control
		public void Setup(string elementname)
		{
			// Keep element name
			this.elementname = elementname;
			
			// Make types list
			fieldtype.Items.Clear();
			fieldtype.Items.AddRange(General.Types.GetCustomUseAttributes());
		}
		
		// This applies last sort order
		private void Sort()
		{
			// Sort
			int sortcolumn = General.Settings.ReadSetting("customfieldssortcolumn", 0);
			int sortorder = General.Settings.ReadSetting("customfieldssortorder", (int)ListSortDirection.Ascending);

			if(sortorder == (int)SortOrder.Ascending)
				fieldslist.Sort(fieldslist.Columns[sortcolumn], ListSortDirection.Ascending);
			else if(sortorder == (int)SortOrder.Descending)
				fieldslist.Sort(fieldslist.Columns[sortcolumn], ListSortDirection.Descending);
		}
		
		// This adds a list of fixed fields (in undefined state)
		public void ListFixedFields(List<UniversalFieldInfo> list)
		{
			// Add all fields
			foreach(UniversalFieldInfo uf in list)
				fieldslist.Rows.Add(new FieldsEditorRow(fieldslist, uf));

			// Sort fields
			Sort();

			// Update new row
			SetupNewRowStyle();
		}
		
		// This sets up the fields and values from a UniFields object
		// When first is true, the values are applied unconditionally
		// When first is false, the values in the grid are erased when
		// they differ from the given values (for multiselection)
		public void SetValues(UniFields fromfields, bool first)
		{
			// Go for all the fields
			foreach(KeyValuePair<string, UniValue> f in fromfields)
			{
				// Go for all rows
				bool foundrow = false;
				foreach(DataGridViewRow row in fieldslist.Rows)
				{
					// Row is a field?
					if(row is FieldsEditorRow)
					{
						FieldsEditorRow frow = row as FieldsEditorRow;
						
						// Row name matches with field
						if(frow.Name == f.Key)
						{
							// First time?
							if(first)
							{
								// Set type when row is not fixed
								if(!frow.IsFixed) frow.ChangeType(f.Value.Type);

								// Apply value of field to row
								frow.Define(f.Value.Value);
							}
							else
							{
								// Check if the value is different
								if(!frow.TypeHandler.GetValue().Equals(f.Value.Value))
								{
									// Clear the value in the row
									frow.Define(f.Value.Value);
									frow.Clear();
								}
							}
							
							// Done
							foundrow = true;
							break;
						}
					}
				}
				
				// Row not found?
				if(!foundrow)
				{
					// Make new row
					FieldsEditorRow frow = new FieldsEditorRow(fieldslist, f.Key, f.Value.Type, f.Value.Value);
					fieldslist.Rows.Insert(fieldslist.Rows.Count - 1, frow);
					
					// When not the first, clear the field
					// because the others did not define this one
					if(!first) frow.Clear();
				}
			}
			
			// Now check for rows that the givens fields do NOT have
			foreach(DataGridViewRow row in fieldslist.Rows)
			{
				// Row is a field?
				if(row is FieldsEditorRow)
				{
					FieldsEditorRow frow = row as FieldsEditorRow;

					// Is this row defined previously?
					if(frow.IsDefined)
					{
						// Check if this row can not be found in the fields at all
						if(!fromfields.ContainsKey(frow.Name))
						{
							// It is not defined in these fields, clear the value
							frow.Clear();
						}
					}
				}
			}
			
			// Sort fields
			Sort();
		}
		
		// This applies the current fields to a UniFields object
		public void Apply(UniFields tofields)
		{
			tofields.BeforeFieldsChange();
			
			// Go for all the fields
			UniFields tempfields = new UniFields(tofields);
			foreach(KeyValuePair<string, UniValue> f in tempfields)
			{
				// Go for all rows
				bool foundrow = false;
				foreach(DataGridViewRow row in fieldslist.Rows)
				{
					// Row is a field and matches field name?
					if((row is FieldsEditorRow) && (row.Cells[0].Value.ToString() == f.Key))
					{
						FieldsEditorRow frow = row as FieldsEditorRow;

						// Field is defined?
						if(frow.IsDefined)
						{
							foundrow = true;
							break;
						}
					}
				}

				// No such row?
				if(!foundrow)
				{
					// Remove the definition from the fields
					tofields.Remove(f.Key);
				}
			}
			
			// Go for all rows
			foreach(DataGridViewRow row in fieldslist.Rows)
			{
				// Row is a field?
				if(row is FieldsEditorRow)
				{
					FieldsEditorRow frow = row as FieldsEditorRow;
					
					// Field is defined and not empty?
					if(frow.IsDefined && !frow.IsEmpty)
					{
						// Apply field
						object oldvalue = null;
						if(tofields.ContainsKey(frow.Name)) oldvalue = tofields[frow.Name].Value;
						tofields[frow.Name] = new UniValue(frow.TypeHandler.Index, frow.GetResult(oldvalue));

						// Custom row?
						if(!frow.IsFixed)
						{
							// Write type to map configuration
							General.Map.Options.SetUniversalFieldType(elementname, frow.Name, frow.TypeHandler.Index);
						}
					}
				}
			}
		}

		#endregion

		#region ================== Events

		// Column header clicked
		private void fieldslist_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// Save sort order
			if(fieldslist.SortedColumn != null)
			{
				int sortcolumn = fieldslist.SortedColumn.Index;
				int sortorder = (int)fieldslist.SortOrder;
				General.Settings.WriteSetting("customfieldssortcolumn", sortcolumn);
				General.Settings.WriteSetting("customfieldssortorder", sortorder);
			}
			
			// Stop any cell editing
			ApplyEnums(true);
			fieldslist.EndEdit();
			HideBrowseButton();
		}
		
		// Resized
		private void FieldsEditorControl_Resize(object sender, EventArgs e)
		{
			// Rearrange controls
			fieldslist.Size = this.ClientSize;
			fieldvalue.Width = fieldslist.ClientRectangle.Width - fieldname.Width - fieldtype.Width - SystemInformation.VerticalScrollBarWidth - 10;
			UpdateBrowseButton();
		}

		// Layout change
		private void FieldsEditorControl_Layout(object sender, LayoutEventArgs e)
		{
			FieldsEditorControl_Resize(sender, EventArgs.Empty);
		}

		// Cell clicked
		private void fieldslist_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			ApplyEnums(true);

			// Anything selected
			if(fieldslist.SelectedCells.Count > 0)
			{
				if(e.RowIndex == fieldslist.NewRowIndex)
					fieldslist.BeginEdit(false);
				else if(e.ColumnIndex > 0)
					fieldslist.BeginEdit(true);
			}
		}

		// Cell doubleclicked
		private void fieldslist_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			FieldsEditorRow frow = null;
			
			// Anything selected
			if(fieldslist.SelectedRows.Count > 0)
			{
				// Get the row
				DataGridViewRow row = fieldslist.Rows[e.RowIndex];
				if(row is FieldsEditorRow) frow = row as FieldsEditorRow;
				
				// First column?
				if(e.ColumnIndex == 0)
				{
					// Not a fixed field?
					if((frow != null) && !frow.IsFixed)
					{
						lasteditfieldname = frow.Name;
						fieldslist.CurrentCell = fieldslist.SelectedRows[0].Cells[0];
						fieldslist.CurrentCell.ReadOnly = false;

						if((e.RowIndex == fieldslist.NewRowIndex) ||
						   frow.Name.StartsWith(FIELD_PREFIX_SUGGESTION, true, CultureInfo.InvariantCulture))
							fieldslist.BeginEdit(false);
						else
							fieldslist.BeginEdit(true);
					}
				}
			}
		}
		
		// User deletes a row
		private void fieldslist_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
		{
			// Get the row
			FieldsEditorRow row = e.Row as FieldsEditorRow;
			
			// Fixed field?
			if(row.IsFixed)
			{
				// Just undefine the field
				row.Undefine();
				e.Cancel = true;
			}
		}

		// User selects a cell for editing
		private void fieldslist_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			// Field name cell?
			if(e.ColumnIndex == 0)
			{
				// New row index?
				if(e.RowIndex == fieldslist.NewRowIndex)
				{
					// Remove all text
					fieldslist.Rows[e.RowIndex].Cells[0].Style.ForeColor = SystemColors.WindowText;
					fieldslist.Rows[e.RowIndex].Cells[0].Value = FIELD_PREFIX_SUGGESTION;
				}
			}
			// Value cell?
			else if(e.ColumnIndex == 2)
			{
				// Get the row
				FieldsEditorRow frow = null;
				DataGridViewRow row = fieldslist.Rows[e.RowIndex];
				if(row is FieldsEditorRow)
				{
					// Get specializedrow
					frow = row as FieldsEditorRow;

					// Enumerable?
					if(frow.TypeHandler.IsEnumerable)
					{
						// Fill combo with enums
						enumscombo.SelectedItem = null;
						enumscombo.Text = "";
						enumscombo.Items.Clear();
						enumscombo.Items.AddRange(frow.TypeHandler.GetEnumList().ToArray());
						enumscombo.Tag = frow;
						
						// Lock combo to enums?
						if(frow.TypeHandler.IsLimitedToEnums)
							enumscombo.DropDownStyle = ComboBoxStyle.DropDownList;
						else
							enumscombo.DropDownStyle = ComboBoxStyle.DropDown;
						
						// Position combobox
						Rectangle cellrect = fieldslist.GetCellDisplayRectangle(2, row.Index, false);
						enumscombo.Location = new Point(cellrect.Left, cellrect.Top);
						enumscombo.Width = cellrect.Width;
						int internalheight = cellrect.Height - (enumscombo.Height - enumscombo.ClientRectangle.Height) - 6;
						General.SendMessage(enumscombo.Handle, General.CB_SETITEMHEIGHT, -1, internalheight);
						
						// Select the value of this field (for DropDownList style combo)
						foreach(EnumItem i in enumscombo.Items)
						{
							// Matches?
							if(string.Compare(i.Title, frow.TypeHandler.GetStringValue(), true, CultureInfo.InvariantCulture) == 0)
							{
								// Select this item
								enumscombo.SelectedItem = i;
							}
						}

						// Put the display text in the text (for DropDown style combo)
						enumscombo.Text = frow.TypeHandler.GetStringValue();
						
						// Show combo
						enumscombo.Show();
					}
				}
			}
		}

		// Done editing cell contents
		private void fieldslist_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			FieldsEditorRow frow = null;
			DataGridViewRow row = null;
			
			// Get the row
			row = fieldslist.Rows[e.RowIndex];
			if(row is FieldsEditorRow) frow = row as FieldsEditorRow;
			
			// Renaming a field?
			if(e.ColumnIndex == 0)
			{
				// Row is a new row?
				if(frow == null)
				{
					// Name given?
					if((row.Cells[0].Value != null) && (row.Cells[0].Value.ToString() != FIELD_PREFIX_SUGGESTION))
					{
						// Make a valid UDMF field name
						string validname = UniValue.ValidateName(row.Cells[0].Value.ToString());
						if(validname.Length > 0)
						{
							// Check if no other row already has this name
							foreach(DataGridViewRow r in fieldslist.Rows)
							{
								// Name matches and not the same row?
								if((r.Index != row.Index) && (r.Cells.Count > 0) && (r.Cells[0].Value != null) &&
								   (r.Cells[0].Value.ToString().ToLowerInvariant() == validname))
								{
									// Cannot have two rows with same name
									validname = "";
									General.ShowWarningMessage("Fields must have unique names!", MessageBoxButtons.OK);
									break;
								}
							}

							// Still valid?
							if(validname.Length > 0)
							{
								// Try to find the type in the map options
								int type = General.Map.Options.GetUniversalFieldType(elementname, validname, 0);

								// Make new row
								frow = new FieldsEditorRow(fieldslist, validname, type, null);
								frow.Visible = false;
								fieldslist.Rows.Insert(e.RowIndex + 1, frow);
							}
						}
					}

					// Mark the row for delete
					row.ReadOnly = true;
					deleterowstimer.Start();
				}
				else
				{
					// Name given?
					if(row.Cells[0].Value != null)
					{
						// Make a valid UDMF field name
						string validname = UniValue.ValidateName(row.Cells[0].Value.ToString());
						if(validname.Length > 0)
						{
							// Check if no other row already has this name
							foreach(DataGridViewRow r in fieldslist.Rows)
							{
								// Name matches and not the same row?
								if((r.Index != row.Index) && (r.Cells.Count > 0) && (r.Cells[0].Value != null) &&
								   (r.Cells[0].Value.ToString().ToLowerInvariant() == validname))
								{
									// Cannot have two rows with same name
									validname = "";
									row.Cells[0].Value = lasteditfieldname;
									General.ShowWarningMessage("Fields must have unique names!", MessageBoxButtons.OK);
									break;
								}
							}

							// Still valid?
							if(validname.Length > 0)
							{
								// Try to find the type in the map options
								int type = General.Map.Options.GetUniversalFieldType(elementname, validname, -1);

								// Rename row and change type
								row.Cells[0].Value = validname;
								if(type != -1) frow.ChangeType(type);
							}
							else
							{
								// Keep old name
								row.Cells[0].Value = lasteditfieldname;
							}
						}
						else
						{
							// Keep old name
							row.Cells[0].Value = lasteditfieldname;
						}
					}
					else
					{
						// Keep old name
						row.Cells[0].Value = lasteditfieldname;
					}
				}
			}
			// Changing field value?
			if((e.ColumnIndex == 2) && (frow != null))
			{
				// Apply changes
				ApplyValue(frow, row.Cells[2].Value);
			}
			
			// Updated
			if(frow != null) frow.CellChanged();
			
			// Update button
			UpdateBrowseButton();
		}

		// Time to delete rows
		private void deleterowstimer_Tick(object sender, EventArgs e)
		{
			// Stop timer
			deleterowstimer.Stop();
			
			// Delete all rows that must be deleted
			for(int i = fieldslist.Rows.Count - 1; i >= 0; i--)
			{
				if(fieldslist.Rows[i].ReadOnly)
					try { fieldslist.Rows.RemoveAt(i); } catch(Exception) { }
				else
					fieldslist.Rows[i].Visible = true;
			}

			// Update new row
			SetupNewRowStyle();
			
			// Update button
			UpdateBrowseButton();
		}

		// Selection changes
		private void fieldslist_SelectionChanged(object sender, EventArgs e)
		{
			browsebutton.Visible = false;
			ApplyEnums(true);
			
			// Update button
			UpdateBrowseButton();
		}

		// Browse clicked
		private void browsebutton_Click(object sender, EventArgs e)
		{
			// Any row selected?
			if(fieldslist.SelectedRows.Count > 0)
			{
				// Get selected row
				DataGridViewRow row = fieldslist.SelectedRows[0];
				if(row is FieldsEditorRow)
				{
					// Browse
					(row as FieldsEditorRow).Browse(this.ParentForm);
				}
			}
		}

		// This handles field data errors
		private void fieldslist_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			// Ignore this, because we want to display values
			// in the type column that are not in their combobox
			e.ThrowException = false;
		}

		// Validate value in enums combobox
		private void enumscombo_Validating(object sender, CancelEventArgs e)
		{
			ApplyEnums(false);
		}

		// Scrolling
		private void fieldslist_Scroll(object sender, ScrollEventArgs e)
		{
			// Stop any cell editing
			ApplyEnums(true);
			fieldslist.EndEdit();
			HideBrowseButton();
		}

		// Mouse up event
		private void fieldslist_MouseUp(object sender, MouseEventArgs e)
		{
			// Focus to enums combobox when visible
			if(enumscombo.Visible)
			{
				enumscombo.Focus();
				enumscombo.SelectAll();
			}
		}
		
		#endregion
		
		#region ================== Private Methods

		// This applies a value to a row
		private void ApplyValue(FieldsEditorRow frow, object value)
		{
			// Defined?
			if((value != null) && (!frow.IsFixed || !frow.Info.Default.Equals(value)))
				frow.Define(value);
			else if(frow.IsFixed)
				frow.Undefine();
		}
		
		// This applies the contents of the enums combobox and hides (if opened)
		private void ApplyEnums(bool hide)
		{
			// Enums combobox shown?
			if((enumscombo.Visible) && (enumscombo.Tag is FieldsEditorRow))
			{
				// Get the row
				FieldsEditorRow frow = (enumscombo.Tag as FieldsEditorRow);

				// Take the selected value and apply it
				ApplyValue(frow, enumscombo.Text);
				
				// Updated
				frow.CellChanged();
			}
			
			if(hide)
			{
				// Hide combobox
				enumscombo.Tag = null;
				enumscombo.Visible = false;
				enumscombo.Items.Clear();
			}
		}
		
		// This sets up the new row
		private void SetupNewRowStyle()
		{
			// Show text for new row
			fieldslist.Rows[fieldslist.NewRowIndex].Cells[0].Value = ADD_FIELD_TEXT;
			fieldslist.Rows[fieldslist.NewRowIndex].Cells[0].Style.ForeColor = SystemColors.GrayText;
			fieldslist.Rows[fieldslist.NewRowIndex].Cells[0].ReadOnly = false;

			// Make sure user can only enter property name in a new row
			fieldslist.Rows[fieldslist.NewRowIndex].Cells[1].ReadOnly = true;
			fieldslist.Rows[fieldslist.NewRowIndex].Cells[2].ReadOnly = true;
		}

		// This hides the browse button
		private void HideBrowseButton()
		{
			browsebutton.Visible = false;
		}

		// This updates the button
		private void UpdateBrowseButton()
		{
			FieldsEditorRow frow = null;
			DataGridViewRow row = null;

			// Any row selected?
			if(fieldslist.SelectedRows.Count > 0)
			{
				// Get selected row
				row = fieldslist.SelectedRows[0];
				if(row is FieldsEditorRow) frow = row as FieldsEditorRow;

				// Not the new row and FieldsEditorRow available?
				if((row.Index < fieldslist.NewRowIndex) && (frow != null))
				{
					// Browse button available for this type?
					if(frow.TypeHandler.IsBrowseable && !frow.TypeHandler.IsEnumerable)
					{
						Rectangle cellrect = fieldslist.GetCellDisplayRectangle(2, row.Index, false);

						// Show button
						enumscombo.Visible = false;
						browsebutton.Location = new Point(cellrect.Right - browsebutton.Width, cellrect.Top);
						browsebutton.Height = cellrect.Height;
						browsebutton.Visible = true;

						/*
						// Determine image/text
						if((frow.Type == UniversalFieldType.SectorEffect) ||
						   (frow.Type == UniversalFieldType.LinedefAction))
						{
							browsebutton.Image = CodeImp.DoomBuilder.Properties.Resources.treeview;
							browsebutton.Text = "";
						}
						else
						{
							browsebutton.Image = null;
							browsebutton.Text = "...";
						}
						*/
					}
					else
					{
						HideBrowseButton();
					}
				}
				else
				{
					HideBrowseButton();
				}
			}
			else
			{
				HideBrowseButton();
			}
		}
		
		#endregion
	}
}
