using Microsoft.AspNetCore.Components;

namespace FileBoy.App;

/// <summary>
/// App component wrapper that references the Blazor UI App.
/// </summary>
public class AppComponent : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<FileBoy.UI.App>(0);
        builder.CloseComponent();
    }
}
