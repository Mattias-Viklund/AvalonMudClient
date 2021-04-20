/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using Avalon.Common.Triggers;
using Avalon.Common.Utilities;
using System;
using System.Windows.Controls;

namespace Avalon.Controls
{
    /// <summary>
    /// The base class that inherits from our editor base and specifies the generic type for
    /// this data entry control.
    /// </summary>
    public class GenericReplacementTriggerListBase : EditorControlBase<ReplacementTrigger>
    {
        public GenericReplacementTriggerListBase(FullyObservableCollection<ReplacementTrigger> source) : base(source)
        {

        }
    }

    /// <summary>
    /// The base class that hides the generic from the partial/class XAML of the <see cref="UserControl"/> we'll
    /// surface on the <see cref="Shell"/>.
    /// </summary>
    public class ReplacementTriggerListBase : GenericReplacementTriggerListBase
    {
        public ReplacementTriggerListBase(FullyObservableCollection<ReplacementTrigger> source) : base(source)
        {

        }
    }

    /// <summary>
    /// Interaction logic for the editor.
    /// </summary>
    public partial class ReplacementTriggerList
    {
        public ReplacementTriggerList(FullyObservableCollection<ReplacementTrigger> source) : base(source)
        {
            InitializeComponent();

            // Set the DataContext to null, we are using the DataContext for the item that has been
            // clicked on that will be edited.
            DataContext = null;
        }

        /// <summary>
        /// The actual filter that's used to filter down the DataGrid.
        /// </summary>
        /// <param name="item"></param>
        public override bool Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(TextFilter.Text))
            {
                return true;
            }

            var trigger = (ReplacementTrigger)item;

            return (trigger?.Pattern?.Contains(TextFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (trigger?.Replacement?.Contains(TextFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false)
                   || (trigger?.Group?.Contains(TextFilter.Text, StringComparison.OrdinalIgnoreCase) ?? false);
        }


        /// <summary>
        /// Close the window with an Ok or success.
        /// </summary>
        public override void PrimaryButtonClick()
        {
        }

        /// <summary>
        /// Close the window with a cancel or close.
        /// </summary>
        public override void SecondaryButtonClick()
        {
        }

        /// <summary>
        /// Event that fires when the selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var v = e.AddedItems[0] as ReplacementTrigger;

                // If it's null that means a new item should be added.
                if (v == null)
                {
                    var trigger = new ReplacementTrigger();
                    App.Settings.ProfileSettings.ReplacementTriggerList.Add(new ReplacementTrigger());

                    try
                    {
                        DataList.SelectedItem = DataList.Items[^2];
                        TextPattern.Focus();
                    }
                    catch {}

                    return;
                }

                // Set the context to the variable that the user has clicked on and will possibly be editing.  The
                // Lua editor needs to know where it's saving to, set that up also.
                this.DataContext = v;
                LuaEditor.SaveObject = v;
                LuaEditor.SaveProperty = "OnMatchEvent";
                LuaEditor.Editor.Text = !string.IsNullOrWhiteSpace(v.OnMatchEvent) ? v.OnMatchEvent : string.Empty;
            }
        }

        /// <summary>
        /// When the cell edit is ending setup all of the triggers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataList_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            Utilities.Utilities.TriggerSetup();
        }
    }
}