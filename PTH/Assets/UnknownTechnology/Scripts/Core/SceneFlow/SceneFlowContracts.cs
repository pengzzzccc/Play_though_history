namespace UnknownTechnology.Core.SceneFlow
{
    public interface ISceneFlowService
    {
        bool IsLoading { get; }
        bool RequestLoad(string routeId);
    }

    public interface ISceneAccessPolicy
    {
        bool CanEnter(string routeId, out string rejectionReason);
    }

    public interface IContinueTargetProvider
    {
        bool TryGetContinueRoute(out string routeId);
    }

    public interface ISceneProgressRestorer
    {
        void Restore(EraSceneContext context);
    }

    public sealed class DefaultSceneAccessPolicy : ISceneAccessPolicy
    {
        public bool CanEnter(string routeId, out string rejectionReason)
        {
            if (routeId == SceneFlowConfig.MainMenuRoute || routeId == SceneFlowConfig.AncientRoute)
            {
                rejectionReason = string.Empty;
                return true;
            }

            rejectionReason = "This era is not unlocked yet.";
            return false;
        }
    }

    public sealed class NoContinueTargetProvider : IContinueTargetProvider
    {
        public bool TryGetContinueRoute(out string routeId)
        {
            routeId = string.Empty;
            return false;
        }
    }

    public sealed class NoOpSceneProgressRestorer : ISceneProgressRestorer
    {
        public void Restore(EraSceneContext context)
        {
        }
    }
}
