using System;
using System.Windows.Forms;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// Finds the controls a Classic dialog built for itself.
    /// </summary>
    /// <remarks>
    /// The Windows Forms designer emits every control as a private field, so a test cannot name one
    /// directly. It also gives each one a Name, which is what the framework's own lookup searches, so
    /// the controls are reached through that rather than through reflection.
    /// <para>
    /// The lookup only sees controls still parented in the dialog, which suits what these tests are
    /// checking: several of the dialogs configure themselves by removing the controls a given set of
    /// options does not call for, so "is it still there" is itself the assertion. That is what
    /// <see cref="Holds"/> is for.
    /// </para>
    /// </remarks>
    internal static class FormControls
    {
        /// <summary>
        /// Finds a named control within a dialog.
        /// </summary>
        /// <typeparam name="TControl">The control's type.</typeparam>
        /// <param name="root">The dialog to search.</param>
        /// <param name="name">The name the designer gave the control.</param>
        /// <returns>The control.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no control of that name is in the dialog, or one is but of a different type.</exception>
        public static TControl Find<TControl>(Control root, string name) where TControl : Control
        {
            ArgumentNullException.ThrowIfNull(root);
            Control[] found = root.Controls.Find(name, searchAllChildren: true);
            return found.Length switch
            {
                0 => throw new InvalidOperationException($"The dialog holds no control named '{name}'. It may have been removed by the options it was built with."),
                1 => found[0] as TControl ?? throw new InvalidOperationException($"The control named '{name}' is a {found[0].GetType().Name} rather than a {typeof(TControl).Name}."),
                _ => throw new InvalidOperationException($"The dialog holds {found.Length} controls named '{name}'."),
            };
        }

        /// <summary>
        /// Whether a named control is still parented in a dialog.
        /// </summary>
        /// <param name="root">The dialog to search.</param>
        /// <param name="name">The name the designer gave the control.</param>
        /// <returns><see langword="true"/> if it is; otherwise, <see langword="false"/>.</returns>
        public static bool Holds(Control root, string name)
        {
            ArgumentNullException.ThrowIfNull(root);
            return root.Controls.Find(name, searchAllChildren: true).Length > 0;
        }

        /// <summary>
        /// Selects an item in a dropdown, as a user would.
        /// </summary>
        /// <remarks>
        /// A method rather than an assignment written into the test, because an assignment inside a
        /// lambda is an expression with a value, and the analysers then disagree with each other about
        /// whether the lambda should have a block body to discard it or an expression body to keep it.
        /// A void call sidesteps that.
        /// </remarks>
        /// <param name="control">The dropdown to select in.</param>
        /// <param name="index">The index to select, or -1 to select nothing.</param>
        public static void Select(ComboBox control, int index)
        {
            ArgumentNullException.ThrowIfNull(control);
            control.SelectedIndex = index;
        }

        /// <summary>
        /// Selects an item in a list, as a user would.
        /// </summary>
        /// <param name="control">The list to select in.</param>
        /// <param name="index">The index to select, or -1 to select nothing.</param>
        public static void Select(ListBox control, int index)
        {
            ArgumentNullException.ThrowIfNull(control);
            control.SelectedIndex = index;
        }

        /// <summary>
        /// Clicks a button, as a user would.
        /// </summary>
        /// <remarks>
        /// Not <c language="csharp">PerformClick</c>, which these tests cannot use. That method does nothing unless the
        /// button reports itself selectable, and selectability is computed up the parent chain to the
        /// form - which reports itself invisible until it has been shown. Since these tests deliberately
        /// never show a dialog, every <c language="csharp">PerformClick</c> would silently do nothing and every assertion
        /// after it would be checking that the dialog had not changed.
        /// <para>
        /// Raising the event on the button directly is the way round it. The handler is the one the
        /// designer wired, so what runs is the dialog's real click handling; only the route in is
        /// different. Reflection is needed because the method that raises the event is protected and
        /// most of the dialogs are sealed, so there is no derived type that could reach it.
        /// </para>
        /// </remarks>
        /// <param name="button">The button to click.</param>
        public static void Click(Button button)
        {
            ArgumentNullException.ThrowIfNull(button);
            NonPublic.Call(button, "OnClick", EventArgs.Empty);
        }
    }
}
