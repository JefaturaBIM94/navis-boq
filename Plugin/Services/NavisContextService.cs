using System;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin.Services
{
    public static class NavisContextService
    {
        public static Document Doc => Autodesk.Navisworks.Api.Application.ActiveDocument;

        public static void EnsureDoc()
        {
            if (Doc == null)
                throw new InvalidOperationException("No hay documento activo en Navisworks.");
        }

        public static void OnUI(Action action)
        {
            Exception captured = null;
            Control ctrl = null;

            try
            {
                var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (h != IntPtr.Zero)
                    ctrl = Control.FromHandle(h);
            }
            catch { }

            if (ctrl != null && ctrl.InvokeRequired)
            {
                ctrl.Invoke(new MethodInvoker(() =>
                {
                    try { action(); }
                    catch (Exception ex) { captured = ex; }
                }));
            }
            else
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            }

            if (captured != null)
                throw captured;
        }
    }
}