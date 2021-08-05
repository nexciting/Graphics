using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnityEngine.Rendering.Universal.Internal
{
    //NOTE: This class is meant to be removed when RTHandles get implemented in urp
    internal sealed class RenderTargetBufferSystem
    {
        RTHandle RTA;
        RTHandle RTB;
        bool m_FirstIsBackBuffer = true;
        RenderTextureDescriptor m_Desc;
        FilterMode m_FilterMode;

        string m_NameA;
        string m_NameB;

        bool m_RTisAllocated = false;

        static bool RTHandleNeedsRealloc(RTHandle handle, in RenderTextureDescriptor descriptor, bool scaled)
        {
            if (handle == null || handle.rt == null)
                return true;
            if (handle.useScaling != scaled)
                return true;
            if (!scaled && (handle.rt.width != descriptor.width || handle.rt.height != descriptor.height))
                return true;
            return
                handle.rt.descriptor.depthBufferBits != descriptor.depthBufferBits ||
                (handle.rt.descriptor.depthBufferBits == (int)DepthBits.None && handle.rt.descriptor.graphicsFormat != descriptor.graphicsFormat) ||
                handle.rt.descriptor.dimension != descriptor.dimension ||
                handle.rt.descriptor.enableRandomWrite != descriptor.enableRandomWrite ||
                handle.rt.descriptor.useMipMap != descriptor.useMipMap ||
                handle.rt.descriptor.autoGenerateMips != descriptor.autoGenerateMips ||
                handle.rt.descriptor.msaaSamples != descriptor.msaaSamples ||
                handle.rt.descriptor.bindMS != descriptor.bindMS ||
                handle.rt.descriptor.useDynamicScale != descriptor.useDynamicScale ||
                handle.rt.descriptor.memoryless != descriptor.memoryless;
        }

        public RenderTargetBufferSystem(string name)
        {
            m_NameA = name + "A";
            m_NameB = name + "B";
        }

        public void Dispose()
        {
            RTA?.Release();
            RTB?.Release();
        }

        public RTHandle PeekBackBuffer()
        {
            return m_FirstIsBackBuffer ? RTA : RTB;
        }

        public RTHandle GetBackBuffer(CommandBuffer cmd)
        {
            if (!m_RTisAllocated)
                Initialize(cmd);
            return m_FirstIsBackBuffer ? RTA : RTB;
        }

        public RTHandle GetFrontBuffer(CommandBuffer cmd)
        {
            if (!m_RTisAllocated)
                Initialize(cmd);
            return m_FirstIsBackBuffer ? RTB : RTA;
        }

        public void Swap()
        {
            m_FirstIsBackBuffer = !m_FirstIsBackBuffer;
        }

        void Initialize(CommandBuffer cmd)
        {
            if (RTHandleNeedsRealloc(RTA, m_Desc, false))
            {
                RTA?.Release();
                RTA = RTHandles.Alloc(m_Desc, filterMode: m_FilterMode, wrapMode: TextureWrapMode.Clamp, name: m_NameA);
                cmd.SetGlobalTexture(RTA.name, RTA);
            }
            if (RTHandleNeedsRealloc(RTB, m_Desc, false))
            {
                RTB?.Release();
                RTB = RTHandles.Alloc(m_Desc, filterMode: m_FilterMode, wrapMode: TextureWrapMode.Clamp, name: m_NameB);
                cmd.SetGlobalTexture(RTB.name, RTB);
            }
            m_RTisAllocated = true;
        }

        public void Clear(CommandBuffer cmd)
        {
            m_FirstIsBackBuffer = true;
        }

        public void SetCameraSettings(CommandBuffer cmd, RenderTextureDescriptor desc, FilterMode filterMode)
        {
            desc.depthBufferBits = 0;
            m_Desc = desc;
            m_FilterMode = filterMode;

            Initialize(cmd);
        }

        public RTHandle GetBufferA()
        {
            return RTA;
        }
    }
}
