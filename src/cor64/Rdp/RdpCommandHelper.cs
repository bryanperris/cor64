using cor64.Rdp.Commands;
using static cor64.Rdp.RdpCommandTypes;

namespace cor64.Rdp
{
    public static class RdpCommandHelper
    {
        public static RdpCommand ResolveType(this RdpCommand command)
        {
            if (command.Type.AssoicatedClassType.HasValue) {
                return command.As(command.Type.AssoicatedClassType.Value);
            }
            else {
                return command;
            }
        }
    }
}