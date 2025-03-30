using Microsoft.AspNetCore.Components.Web;
using RenPyReader.DataModels;
using RenPyReader.Utilities;

namespace RenPyReader.Components.Pages
{
    public partial class EventReader
    {
        EventDisplay? EventDisplay { get; set; }

        private string EventNameInput { get; set; } = string.Empty;

        private bool IsEventSearch { get; set; } = true;

        private bool IsEventDisplay { get; set; } = false;

        private bool IsPreviousButtonDisabled { get; set; } = false;

        private bool IsNextButtonDisabled { get; set; } = false;

        private RenPyEvent? RenPyEvent { get; set; }

        private string? ErrorMessage { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                EventDisplay = new EventDisplay(SQLiteService, AudioPlayerService);
                EventDisplay.StateHasChangedCallback += OnStateHasChanged;
                IsPreviousButtonDisabled = EventDisplay.IsBeginning;
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (string.IsNullOrEmpty(EventNameInput))
            {
                return;
            }

            ErrorMessage = string.Empty;
            if (e.Key == "Enter")
            {
                try
                {
                    RenPyEvent = await SQLiteService.GetRenPyEventAsync(EventNameInput);
                    if (RenPyEvent == null)
                    {
                        ErrorMessage = "No event found!";
                    }
                    else
                    {
                        await EventDisplay!.SetEventAndInitialize(RenPyEvent);
                        IsEventSearch = false;
                        IsEventDisplay = true;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    StateHasChanged();
                }
            }
        }

        private async Task GoToNextDialogue()
        {
            IsNextButtonDisabled = true;
            IsPreviousButtonDisabled = true;
            StateHasChanged();

            try
            {
                await EventDisplay!.GoToNextDialogue();
            }
            finally
            {
                if (EventDisplay!.IsEnd)
                {
                    IsNextButtonDisabled = true;
                }
                else
                {
                    IsNextButtonDisabled = false;
                }

                IsPreviousButtonDisabled = false;
                StateHasChanged();
            }
        }

        private async Task GoToPreviousDialogue()
        {
            IsNextButtonDisabled = true;
            IsPreviousButtonDisabled = true;
            StateHasChanged();

            try
            {
                await EventDisplay!.GoToPreviousDialogue();
            }
            finally
            {
                if (EventDisplay!.IsBeginning)
                {
                    IsPreviousButtonDisabled = true;
                }
                else
                {
                    IsPreviousButtonDisabled = false;
                }

                IsNextButtonDisabled = false;
                StateHasChanged();
            }
        }

        public void OnStateHasChanged()
        {
            StateHasChanged();
        }
    }
}