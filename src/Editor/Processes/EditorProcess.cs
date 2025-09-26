
namespace Editor.Processes;

internal abstract class EditorProcess
{
    public string ErrorMessage = "";

    public abstract bool IsValid();

    /// <summary>
    /// 
    /// </summary>
    /// <returns>true if process is over</returns>
    public abstract bool Render();
}