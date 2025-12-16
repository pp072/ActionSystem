using Cysharp.Threading.Tasks;

namespace ActionSystem
{
    public interface IActionItem
    {
        void Init();
        UniTask<bool> Run();
        string Name { get; }
    }
}