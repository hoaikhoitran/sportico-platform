using SporticoApp.Application.DTOs.Advisory;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class AdvisoryMappingExtensions
    {
        public static AdvisoryMessageContext ToContext(this AdvisoryMessage message)
        {
            return new AdvisoryMessageContext
            {
                Sender = message.Sender,
                Content = message.Content
            };
        }
    }
}
