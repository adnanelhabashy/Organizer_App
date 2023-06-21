using Microsoft.AspNetCore.Components;

namespace Organizer_App.Client.Controls
{
    public class ValidationInputBase : ComponentBase
    {
        [Parameter]
        public string placeHolder { get; set; }
        [Parameter]
        public string Value { get; set; }
        [Parameter]
        public EventCallback<string> ValueChangedCallBack { get; set; }

        public async void HandleInputChange(ChangeEventArgs args)
        {
           await ValueChangedCallBack.InvokeAsync(args.Value.ToString());
        }

    }
}
