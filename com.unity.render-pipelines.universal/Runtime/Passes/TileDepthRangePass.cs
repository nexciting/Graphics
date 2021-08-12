using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Calculate min and max depth per screen tile for tiled-based deferred shading.
    /// </summary>
    internal class TileDepthRangePass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;
        int m_PassIndex = 0;

        public TileDepthRangePass(RenderPassEvent evt, DeferredLights deferredLights, int passIndex)
        {
            base.profilingSampler = new ProfilingSampler(nameof(TileDepthRangePass));
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
            m_PassIndex = passIndex;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RTHandle outputTex;
            if (m_PassIndex == 0 && m_DeferredLights.HasTileDepthRangeExtraPass())
                outputTex = m_DeferredLights.DepthInfoTexture;
            else
                outputTex = m_DeferredLights.TileDepthInfoTexture;
            cmd.SetGlobalTexture(outputTex.name, outputTex.nameID);
            base.ConfigureTarget(outputTex);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_PassIndex == 0)
                m_DeferredLights.ExecuteTileDepthInfoPass(context, ref renderingData);
            else
                m_DeferredLights.ExecuteDownsampleBitmaskPass(context, ref renderingData);
        }
    }
}
