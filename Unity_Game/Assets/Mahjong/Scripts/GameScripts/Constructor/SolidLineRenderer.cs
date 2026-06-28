using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class SolidLineRenderer : MonoBehaviour
    {
        [SerializeField]
        private float width = 0.15f;
        [SerializeField]
        private Material material;
        [SerializeField]
        private int sortingOrder = 0;

        #region temp vars
        private LineRenderer lineRenderer;
        private Vector3 offset;
        private Vector3 sourcePos_1;
        private Vector3 sourcePos_2;
        #endregion temp vars

        public SolidLineRenderer Create (Transform parent, Vector3 pos1, Vector3 pos2)
        {
            Material mat = (!material) ? new Material(Shader.Find("Sprites/Default")) : material;
            
            SolidLineRenderer sLR = Instantiate(this, parent.transform);
            if (!sLR) return null;
            sLR.sourcePos_1 = pos1;
            sLR.sourcePos_2 = pos2;
            sLR.lineRenderer = sLR.GetComponent<LineRenderer>();
            sLR.lineRenderer.material = mat;
            sLR.lineRenderer.startWidth = width;
            sLR.lineRenderer.endWidth = width;
            sLR.lineRenderer.startColor = new Color(1,0,0,0.3f);
            sLR.lineRenderer.endColor = new Color(1, 0, 0, 0.3f);
            sLR.lineRenderer.sortingOrder = sortingOrder ;

            Vector3 [] positions = new Vector3 [] {pos1, pos2 }; // world pos
            sLR.lineRenderer.positionCount = positions.Length;
            sLR.lineRenderer.SetPositions(positions);
            return sLR;
        }

        /// <summary>
        /// Enable or disable line elemnts.
        /// </summary>
        internal void SetLineVisible(bool visible)
        {
            if (lineRenderer) lineRenderer.enabled = visible;
        }

        public void SetOffset(Vector3 offset)
        {
            this.offset = offset;
            Vector3[] positions = new Vector3[] { sourcePos_1 + offset, sourcePos_2 + offset }; // world pos
            lineRenderer.SetPositions(positions);
        }

        #region private
        /// <summary>
        /// Set Order for line spite rendrer.
        /// </summary>
        private void SetLineRenderOrder(int order)
        {
            if (lineRenderer) lineRenderer.sortingOrder = order;
        }
        #endregion private
    }
}