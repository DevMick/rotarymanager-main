using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Application.DTOs.Authentication
{
    public class ClubDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }
}
