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

        int m_NameA;
        int m_NameB;

        bool m_RTisAllocated = false;

        public RenderTargetBufferSystem(string name)
        {
            string nameA = name + "A";
            string nameB = name + "B";
            m_NameA = Shader.PropertyToID(nameA);
            m_NameB = Shader.PropertyToID(nameB);
            RTA = RTHandles.Alloc(new RenderTargetIdentifier(m_NameA, 0, CubemapFace.Unknown, -1), nameA);
            RTB = RTHandles.Alloc(new RenderTargetIdentifier(m_NameB, 0, CubemapFace.Unknown, -1), nameB);
        }

        public void Dispose()
        {
            RTA?.Release();
            RTB?.Release();
        }

        public RTHandle GetBackBuffer()
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
            cmd.GetTemporaryRT(m_NameA, m_Desc, m_FilterMode);
            var descB = m_Desc;
            descB.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_NameB, m_Desc, m_FilterMode);
            m_RTisAllocated = true;
        }

        public void Clear(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_NameA);
            cmd.ReleaseTemporaryRT(m_NameB);

            m_FirstIsBackBuffer = true;
        }

        public void SetCameraSettings(CommandBuffer cmd, RenderTextureDescriptor desc, FilterMode filterMode)
        {
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
