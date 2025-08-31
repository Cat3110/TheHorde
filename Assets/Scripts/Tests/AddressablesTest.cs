using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

// новый инпут

namespace Tests
{
    public class AddressablesTest : MonoBehaviour
    {
        private GameObject _spawnedObj;
        private AsyncOperationHandle<GameObject> _handle;

        private InputAction _loadAction;
        private InputAction _unloadAction;

        void OnEnable()
        {
            _loadAction = new InputAction("LoadCube", binding: "<Keyboard>/a");
            _unloadAction = new InputAction("UnloadCube", binding: "<Keyboard>/d");

            _loadAction.performed += _ => LoadCube();
            _unloadAction.performed += _ => UnloadCube();

            _loadAction.Enable();
            _unloadAction.Enable();
        }

        void OnDisable()
        {
            _loadAction.Disable();
            _unloadAction.Disable();
        }

        public void LoadCube()
        {
            _handle = Addressables.LoadAssetAsync<GameObject>("Cube");
            _handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    _spawnedObj = Instantiate(obj.Result, Vector3.zero, Quaternion.identity);
                    Debug.Log("Cube загружен и создан");
                }
            };
        }

        public void UnloadCube()
        {
            if (_spawnedObj != null)
            {
                Destroy(_spawnedObj);
                _spawnedObj = null;
            }
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                Debug.Log("Cube выгружен");
            }
        }
    }
}