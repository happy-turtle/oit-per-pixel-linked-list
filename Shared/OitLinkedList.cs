using UnityEngine;
using UnityEngine.Rendering;

namespace OrderIndependentTransparency
{
    public class OitLinkedList : IOrderIndependentTransparency
    {
        private int screenWidth, screenHeight;
        private ComputeBuffer fragmentLinkBuffer;
        private readonly int fragmentLinkBufferId;
        private ComputeBuffer startOffsetBuffer;
        private readonly int startOffsetBufferId;
        private readonly Material linkedListMaterial;
        private const int MAX_SORTED_PIXELS = 8;

        private readonly ComputeShader oitComputeUtils;
        private readonly int clearStartOffsetBufferKernel;
        private int dispatchGroupSizeX, dispatchGroupSizeY;

        public OitLinkedList(string shaderName)
        {
            linkedListMaterial = new Material(Resources.Load<Shader>(shaderName));
            fragmentLinkBufferId = Shader.PropertyToID("FLBuffer");
            startOffsetBufferId = Shader.PropertyToID("StartOffsetBuffer");

            oitComputeUtils = Resources.Load<ComputeShader>("OitComputeUtils");
            clearStartOffsetBufferKernel = oitComputeUtils.FindKernel("ClearStartOffsetBuffer");

            if (screenWidth != 0 && screenHeight != 0)
            {
                SetupGraphicsBuffers();
            }
        }

        public void PreRender(CommandBuffer command)
        {
            // validate the effect itself
            if (Screen.width != screenWidth || Screen.height != screenHeight)
            {
                SetupGraphicsBuffers();
            }

            //reset StartOffsetBuffer to zeros
            oitComputeUtils.SetBuffer(clearStartOffsetBufferKernel, startOffsetBufferId, startOffsetBuffer);
            oitComputeUtils.SetInt("screenWidth", screenWidth);
            oitComputeUtils.Dispatch(clearStartOffsetBufferKernel, dispatchGroupSizeX, dispatchGroupSizeY, 1);

            // set buffers for rendering
            command.SetRandomWriteTarget(1, fragmentLinkBuffer);
            command.SetRandomWriteTarget(2, startOffsetBuffer);
        }

        public Material Render(CommandBuffer command, RenderTargetIdentifier src, RenderTargetIdentifier dest)
        {
            command.ClearRandomWriteTargets();
            if (linkedListMaterial == null)
            {
                return null;
            }
            // blend linked list
            linkedListMaterial.SetBuffer(fragmentLinkBufferId, fragmentLinkBuffer);
            linkedListMaterial.SetBuffer(startOffsetBufferId, startOffsetBuffer);
            return linkedListMaterial;
        }

        public void Release()
        {
            fragmentLinkBuffer?.Dispose();
            startOffsetBuffer?.Dispose();
        }

        private void SetupGraphicsBuffers()
        {
            Release();
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            int bufferSize = screenWidth * screenHeight * MAX_SORTED_PIXELS;
            int bufferStride = sizeof(uint) * 3;
            //the structured buffer contains all information about the transparent fragments
            //this is the per pixel linked list on the gpu
            fragmentLinkBuffer = new ComputeBuffer(bufferSize, bufferStride, ComputeBufferType.Counter);

            int bufferSizeHead = screenWidth * screenHeight;
            int bufferStrideHead = sizeof(uint);
            //create buffer for addresses, this is the head of the linked list
            startOffsetBuffer = new ComputeBuffer(bufferSizeHead, bufferStrideHead, ComputeBufferType.Raw);

            dispatchGroupSizeX = Mathf.CeilToInt(screenWidth / 32.0f);
            dispatchGroupSizeY = Mathf.CeilToInt(screenHeight / 32.0f);
        }
    }
}