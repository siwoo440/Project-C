using System.Collections; // 실제 화면 프레임 대기 사용
using NUnit.Framework; // 툴팁 화면 범위 검사 사용
using UnityEngine; // 화면 좌표와 전투 객체 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
public sealed class BattleTooltipPlacementTests // 상태 상세 화면 위치 검사
{ // 툴팁 위치 검사 시작
    [UnityTest] // 화면 모서리 상태 상세 검사 지정
    public IEnumerator StatusTooltip_StaysWithinCanvasNearTopLeft() // 화면 위쪽 유닛의 상태 상세가 잘리지 않는지 확인
    { // 툴팁 화면 범위 검사 시작
        yield return new EnterPlayMode(); // 실제 화면 플레이 시작
        GameObject canvasObject = new GameObject("TooltipCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 전투 캔버스 조회
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 실제 화면 좌표 캔버스 설정
        GameObject unitObject = new GameObject("Unit", typeof(RectTransform), typeof(BattleStatusEffectView)); // 화면 가장자리 유닛 생성
        unitObject.transform.SetParent(canvasObject.transform, false); // 유닛을 전투 캔버스에 연결
        RectTransform unitRect = unitObject.GetComponent<RectTransform>(); // 유닛 화면 영역 조회
        unitRect.anchorMin = new Vector2(0.02f, 0.98f); // 왼쪽 위 최소 기준 설정
        unitRect.anchorMax = new Vector2(0.02f, 0.98f); // 왼쪽 위 최대 기준 설정
        unitRect.sizeDelta = new Vector2(160f, 280f); // 실제 유닛 화면 크기 설정
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>(); // 임시 캐릭터 데이터 생성
        BattleUnitRuntime unit = BattleUnitRuntime.CreateAlly(data); // 실제 전투 유닛 생성
        unit.ApplyStatusEffect(BattleStatusEffectType.Poison, 5, 3, 1); // 표시할 중독 상태 적용
        BattleStatusEffectView status = unitObject.GetComponent<BattleStatusEffectView>(); // 실제 상태 화면 조회
        status.Bind(unit); // 실제 전투 유닛 상태 연결
        Canvas.ForceUpdateCanvases(); // 화면 영역 배치 갱신
        status.ShowTooltip(); // 실제 상태 상세 화면 표시
        yield return null; // 툴팁 위치 보정 프레임 대기
        Canvas.ForceUpdateCanvases(); // 보정된 화면 영역 갱신
        RectTransform tooltip = status.TooltipRect; // 원래 상태 화면의 실제 상세 영역 조회
        Vector3[] corners = new Vector3[4]; // 툴팁 화면 꼭짓점 저장 공간
        tooltip.GetWorldCorners(corners); // 실제 상태 상세 꼭짓점 조회
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>(); // 전체 화면 영역 조회
        Vector3 minimum = canvasRect.InverseTransformPoint(corners[0]); // 툴팁 왼쪽 아래 화면 좌표 변환
        Vector3 maximum = canvasRect.InverseTransformPoint(corners[2]); // 툴팁 오른쪽 위 화면 좌표 변환
        Assert.GreaterOrEqual(minimum.x, canvasRect.rect.xMin - 0.1f); // 툴팁 왼쪽 잘림 방지 확인
        Assert.GreaterOrEqual(minimum.y, canvasRect.rect.yMin - 0.1f); // 툴팁 아래쪽 잘림 방지 확인
        Assert.LessOrEqual(maximum.x, canvasRect.rect.xMax + 0.1f); // 툴팁 오른쪽 잘림 방지 확인
        Assert.LessOrEqual(maximum.y, canvasRect.rect.yMax + 0.1f); // 툴팁 위쪽 잘림 방지 확인
        unitRect.anchorMin = new Vector2(0.98f, 0.02f); // 오른쪽 아래 최소 기준 설정
        unitRect.anchorMax = new Vector2(0.98f, 0.02f); // 오른쪽 아래 최대 기준 설정
        Canvas.ForceUpdateCanvases(); // 이동한 유닛의 화면 배치 갱신
        status.ShowTooltip(); // 오른쪽 아래 상태 상세 다시 표시
        tooltip.GetWorldCorners(corners); // 보정된 상세 영역 꼭짓점 조회
        minimum = canvasRect.InverseTransformPoint(corners[0]); // 상세 왼쪽 아래 화면 좌표 변환
        maximum = canvasRect.InverseTransformPoint(corners[2]); // 상세 오른쪽 위 화면 좌표 변환
        Assert.GreaterOrEqual(minimum.x, canvasRect.rect.xMin - 0.1f); // 오른쪽 아래 상황의 왼쪽 잘림 방지 확인
        Assert.GreaterOrEqual(minimum.y, canvasRect.rect.yMin - 0.1f); // 오른쪽 아래 상황의 아래 잘림 방지 확인
        Assert.LessOrEqual(maximum.x, canvasRect.rect.xMax + 0.1f); // 오른쪽 아래 상황의 오른쪽 잘림 방지 확인
        Assert.LessOrEqual(maximum.y, canvasRect.rect.yMax + 0.1f); // 오른쪽 아래 상황의 위쪽 잘림 방지 확인
        Object.DestroyImmediate(canvasObject); // 독립 전투 화면 정리
        Object.DestroyImmediate(data); // 임시 캐릭터 데이터 정리
        yield return new ExitPlayMode(); // 실제 화면 플레이 종료
    } // 툴팁 화면 범위 검사 종료
} // 툴팁 위치 검사 종료
