using Dalamud.Plugin;

namespace MakePlacePlugin.Gui
{
    public abstract class Window<T> where T : IDalamudPlugin
    {
        // 移除了控制可见性、上传、导入的相关属性
        // protected bool WindowVisible;
        // protected bool WindowCanUpload;
        // protected bool WindowCanImport;

        // public virtual bool Visible
        // {
        //     get => WindowVisible;
        //     set => WindowVisible = value;
        // }
        // public virtual bool CanUpload
        // {
        //     get => WindowCanUpload;
        //     set => WindowCanUpload = value;
        // }
        // public virtual bool CanImport
        // {
        //     get => WindowCanImport;
        //     set => WindowCanImport = value;
        // }

        protected T Plugin { get; }

        protected Window(T plugin)
        {
            Plugin = plugin;
        }

        public void Draw()
        {
            // 直接调用 DrawUi 和 DrawScreen 方法，不再根据可见性判断
            DrawUi();
            DrawScreen();
        }

        protected abstract void DrawUi();
        protected abstract void DrawScreen();
    }
}