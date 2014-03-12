using DoubleAgent.Control;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi
{
    internal interface ICharacterEvents
    {
        void OnRequestStart(Request request);
        void OnRequestComplete(Request request);
        void OnMove(short x, short y, MoveCauseType cause);
        void OnClick(short button, bool shiftKey, short x, short y);
        void OnCommand(UserInput userInput);
        void OnDoubleClick(short button, bool shiftKey, short x, short y);
        void OnDragStart(short button, bool shiftKey, short x, short y);
        void OnDragComplete(short button, bool shiftKey, short x, short y);
        void OnHide(VisibilityCauseType cause);
        void OnShow(VisibilityCauseType cause);
    }
}