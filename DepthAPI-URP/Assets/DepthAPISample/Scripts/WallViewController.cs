using UnityEngine;

namespace DepthAPISample
{
    public class WallViewController : MonoBehaviour
    {
        [SerializeField] private OVRInput.RawButton _wallsVisibilityToggleButton = OVRInput.RawButton.Y;
        private bool _areWallsVisible;

        private MeshRenderer _meshRenderer;
        private Material _material;
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _material = _meshRenderer.material;
            if (_material != null)
            {
                AdjustTextureTiling();
            }
        }

        private void AdjustTextureTiling()
        {
            _material.mainTextureScale = new Vector2(transform.localScale.x, transform.localScale.y);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                _areWallsVisible = !_areWallsVisible;
                SetWallsVisible(_areWallsVisible);
            }
            if (OVRInput.GetDown(_wallsVisibilityToggleButton))
            {
                _areWallsVisible = !_areWallsVisible;
                SetWallsVisible(_areWallsVisible);
            }
        }

        private void SetWallsVisible(bool isOn)
        {
            _meshRenderer.enabled = isOn;
        }
    }
}
