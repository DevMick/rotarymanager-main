using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Infrastructure.Services
{
    public interface ITenantService
    {
        Guid GetCurrentTenantId();
        void SetCurrentTenantId(Guid tenantId);
    }
}
