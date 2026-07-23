using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // 포인터 이벤트 기능
public sealed class BattleTargetView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // 전투 대상 선택 표시
{
    [Header("전투 대상 연결")] // 전투 대상 연결 구분
    [SerializeField] private BattleUnit battleUnit; // 연결된 전투 유닛
    [SerializeField] private BattleTargetSelectionRuntime targetSelectionRuntime; // 대상 선택 관리자
    [SerializeField] private LineRenderer targetOutline; // 사각형 대상 윤곽

    [Header("대상 표시 색상")] // 대상 표시 색상 구분
    [SerializeField] private Color availableColor = new Color(1f, 0.8f, 0.2f, 1f); // 선택 가능 색상
    [SerializeField] private Color hoverColor = new Color(0.2f, 1f, 1f, 1f); // 마우스 강조 색상

    private bool isTargetable; // 현재 선택 가능 여부

    public BattleUnit BattleUnit => battleUnit; // 연결된 전투 유닛 반환

    private void Awake() // 대상 연결 초기화
    {
        if (battleUnit == null) // 전투 유닛 연결 확인
        {
            battleUnit = GetComponent<BattleUnit>(); // 현재 오브젝트 유닛 검색
        }

        if (targetSelectionRuntime == null) // 대상 관리자 연결 확인
        {
            targetSelectionRuntime = FindFirstObjectByType<BattleTargetSelectionRuntime>(); // 씬 대상 관리자 검색
        }

        if (targetOutline != null) // 윤곽 연결 확인
        {
            targetOutline.enabled = false; // 시작 윤곽 숨김
        }
    }

    private void OnEnable() // 대상 관리자 등록
    {
        if (targetSelectionRuntime != null) // 대상 관리자 연결 확인
        {
            targetSelectionRuntime.RegisterTarget(this); // 현재 유닛 대상 등록
        }
    }

    private void OnDisable() // 대상 관리자 해제
    {
        if (targetSelectionRuntime != null) // 대상 관리자 연결 확인
        {
            targetSelectionRuntime.UnregisterTarget(this); // 현재 유닛 대상 해제
        }
    }

    public void SetTargetable(bool targetable) // 선택 가능 상태 적용
    {
        isTargetable = targetable; // 선택 가능 여부 저장

        if (targetOutline == null) // 윤곽 연결 확인
        {
            return; // 윤곽 처리 중단
        }

        targetOutline.enabled = isTargetable; // 윤곽 표시 상태 적용

        if (isTargetable) // 선택 가능 상태 확인
        {
            SetOutlineColor(availableColor); // 기본 선택 가능 색상 적용
        }
    }

    public void OnPointerEnter(PointerEventData eventData) // 마우스 진입 처리
    {
        if (isTargetable == false) // 선택 가능 여부 확인
        {
            return; // 마우스 강조 중단
        }

        SetOutlineColor(hoverColor); // 마우스 강조 색상 적용
    }

    public void OnPointerExit(PointerEventData eventData) // 마우스 이탈 처리
    {
        if (isTargetable == false) // 선택 가능 여부 확인
        {
            return; // 기본 색상 복구 중단
        }

        SetOutlineColor(availableColor); // 기본 선택 가능 색상 복구
    }

    public void OnPointerClick(PointerEventData eventData) // 대상 클릭 처리
    {
        if (eventData.button != PointerEventData.InputButton.Left) // 마우스 왼쪽 버튼 확인
        {
            return; // 다른 버튼 입력 중단
        }

        Debug.Log($"Enemy clicked: {gameObject.name}", this); // 적 클릭 확인 로그

        if (isTargetable == false) // 선택 가능 상태 확인
        {
            Debug.LogWarning("Clicked enemy is not targetable.", this); // 선택 불가 상태 경고
            return; // 대상 클릭 중단
        }

        if (targetSelectionRuntime == null) // 대상 관리자 연결 확인
        {
            Debug.LogError("Target selection runtime is missing.", this); // 대상 관리자 누락 오류
            return; // 대상 클릭 중단
        }

        bool selected = targetSelectionRuntime.TrySelectTarget(this); // 현재 적 선택 요청
        Debug.Log($"Target selection result: {selected}", this); // 대상 선택 결과 출력
    }

    private void SetOutlineColor(Color color) // 윤곽 색상 적용
    {
        if (targetOutline == null) // 윤곽 연결 확인
        {
            return; // 색상 적용 중단
        }

        targetOutline.startColor = color; // 윤곽 시작 색상 적용
        targetOutline.endColor = color; // 윤곽 끝 색상 적용
    }
}