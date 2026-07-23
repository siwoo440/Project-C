using System.Collections; // 코루틴 자료형
using UnityEngine; // Unity 기본 기능

public sealed class BattleUnitDefeatView : MonoBehaviour // 전투 불능 연출 관리자
{
    [Header("전투 유닛 연결")] // 전투 유닛 연결 구분
    [SerializeField] private BattleUnit battleUnit; // 연결된 전투 유닛
    [SerializeField] private BattleTargetView targetView; // 대상 선택 표시
    [SerializeField] private Collider2D targetCollider; // 대상 클릭 충돌체
    [SerializeField] private SpriteRenderer unitSpriteRenderer; // 유닛 이미지
    [SerializeField] private GameObject healthUIRoot; // 체력 UI 최상위 오브젝트

    [Header("전투 불능 연출")] // 전투 불능 연출 구분
    [SerializeField, Min(0f)] private float fadeDuration = 0.4f; // 사라지는 시간

    private Coroutine defeatCoroutine; // 진행 중인 처치 연출

    private void Awake() // 필수 컴포넌트 자동 검색
    {
        if (battleUnit == null) // 전투 유닛 연결 확인
        {
            battleUnit = GetComponent<BattleUnit>(); // 현재 오브젝트 유닛 검색
        }

        if (targetView == null) // 대상 표시 연결 확인
        {
            targetView = GetComponent<BattleTargetView>(); // 현재 오브젝트 대상 표시 검색
        }

        if (targetCollider == null) // 클릭 충돌체 연결 확인
        {
            targetCollider = GetComponent<Collider2D>(); // 현재 오브젝트 충돌체 검색
        }

        if (unitSpriteRenderer == null) // 유닛 이미지 연결 확인
        {
            unitSpriteRenderer = GetComponent<SpriteRenderer>(); // 현재 오브젝트 이미지 검색
        }
    }

    private void OnEnable() // 전투 불능 이벤트 연결
    {
        if (battleUnit != null) // 전투 유닛 연결 확인
        {
            battleUnit.Defeated += HandleDefeated; // 전투 불능 이벤트 등록
        }
    }

    private void OnDisable() // 전투 불능 이벤트 해제
    {
        if (battleUnit != null) // 전투 유닛 연결 확인
        {
            battleUnit.Defeated -= HandleDefeated; // 전투 불능 이벤트 제거
        }
    }

    private void HandleDefeated(BattleUnit defeatedUnit) // 전투 불능 처리
    {
        if (defeatedUnit != battleUnit) // 연결된 유닛 여부 확인
        {
            return; // 다른 유닛 처리 중단
        }

        if (targetView != null) // 대상 표시 연결 확인
        {
            targetView.SetTargetable(false); // 대상 선택 표시 해제
        }

        if (targetCollider != null) // 클릭 충돌체 연결 확인
        {
            targetCollider.enabled = false; // 추가 클릭 차단
        }

        if (healthUIRoot != null) // 체력 UI 연결 확인
        {
            healthUIRoot.SetActive(false); // 체력 UI 숨김
        }

        if (defeatCoroutine != null) // 기존 연출 진행 여부 확인
        {
            StopCoroutine(defeatCoroutine); // 기존 연출 중단
        }

        defeatCoroutine = StartCoroutine(PlayDefeatAnimation()); // 페이드아웃 연출 시작
    }

    private IEnumerator PlayDefeatAnimation() // 전투 불능 페이드아웃
    {
        if (unitSpriteRenderer == null) // 유닛 이미지 연결 확인
        {
            yield break; // 연출 중단
        }

        Color startColor = unitSpriteRenderer.color; // 시작 이미지 색상 저장

        if (fadeDuration <= 0f) // 즉시 숨김 설정 확인
        {
            unitSpriteRenderer.enabled = false; // 유닛 이미지 숨김
            yield break; // 연출 종료
        }

        float elapsedTime = 0f; // 진행 시간 초기화

        while (elapsedTime < fadeDuration) // 페이드 시간 진행 확인
        {
            elapsedTime += Time.deltaTime; // 프레임 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration); // 진행 비율 계산
            float currentAlpha = Mathf.Lerp(startColor.a, 0f, normalizedTime); // 현재 투명도 계산
            unitSpriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, currentAlpha); // 이미지 투명도 적용
            yield return null; // 다음 프레임 대기
        }

        unitSpriteRenderer.enabled = false; // 연출 완료 이미지 숨김
        defeatCoroutine = null; // 진행 코루틴 정보 제거
    }
}