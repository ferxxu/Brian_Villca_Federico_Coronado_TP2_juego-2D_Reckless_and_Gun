using reckless_and_gun.Scenes;
using MonoGameLibrary;

namespace reckless_and_gun;

public class Game1 : Core
{
    public Game1() : base("Reckless and Gun", 1280, 600, false)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();
        ChangeScene(new TitleScene());
    }

    protected override void LoadContent()
    {
    }
}
