using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private Guid _currentTenantId = Guid.Empty;

        // Utilisation d'un AsyncLocal pour stocker l'ID du tenant par contexte de requête
        private static readonly AsyncLocal<Guid> _tenantId = new();

        public Guid GetCurrentTenantId()
        {
            return _tenantId.Value;
        }

        public void SetCurrentTenantId(Guid tenantId)
        {
            _tenantId.Value = tenantId;
        }
    }
}
