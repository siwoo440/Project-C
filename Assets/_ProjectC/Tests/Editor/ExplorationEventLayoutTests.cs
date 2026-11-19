using System.Collections; // 플레이 모드 전환 대기
using System.Linq; // 잿더미 원본 조회
using NUnit.Framework; // 배치 경계 비교
using UnityEngine; // 실제 화면 좌표 조회
using UnityEngine.TestTools; // 플레이 모드 검사 실행
using UnityEngine.UI; // 레이아웃 갱신

public sealed class ExplorationEventLayoutTests // 탐사 이벤트 패널 배치 검사
{ // 배치 검사 시작
    [UnityTest] // 실제 캔버스 검사 지정
    public IEnumerator PanelFitsGuideAndKeepsAllChoicesBelowStory() // 패널 여백과 선택지 경계 확인
    { // 검사 준비 시작
        yield return new EnterPlayMode(); // 게임 화면으로 전환
        var panel = ExplorationEventPanelView.EnsureInstance(); // 공용 이벤트 화면 준비
        var data = ExplorationEventCatalog.LoadEvents().Single(item => item.EventId == AshPileEvent.Id); // 선택지 세 개의 원본 조회
        var marker = new GameObject("EventLayoutMarker").AddComponent<ExplorationEventView>(); // 탐사 표식 준비
        marker.Initialize(data, "layout_check"); // 표식에 원본 연결
        panel.ShowEvent(marker, data, marker.RuntimeEventId); // 탐사 이벤트 열기
        panel.SendMessage("PopulateEventContent"); // 화면 캡처 대기 없이 실제 내용 채우기
        var root = (RectTransform)panel.transform.Find("EventPanel"); // 실제 표시 패널 조회
        root.gameObject.SetActive(true); // 검사 프레임의 본문 표시
        Canvas.ForceUpdateCanvases(); // 부모 캔버스 크기 확정
        LayoutRebuilder.ForceRebuildLayoutImmediate(root); // 선택지 세로 배치 확정
        var canvas = (RectTransform)panel.transform; // 전체 게임 화면 경계 조회
        var outer = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas, root); // 자식까지 포함한 표시 범위 계산
        Assert.That(outer.size.x / canvas.rect.width, Is.EqualTo(0.954f).Within(0.004f)); // 참고 화면 패널 너비 확인
        Assert.That(outer.size.y / canvas.rect.height, Is.EqualTo(0.848f).Within(0.004f)); // 참고 화면 패널 높이 확인
        Assert.That((outer.min.y - canvas.rect.yMin) / canvas.rect.height, Is.EqualTo(0.117f).Within(0.004f)); // 아래 파티 정보 여백 확인
        var body = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Content/BodyText")); // 본문 경계 조회
        var art = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Content/IllustrationFrame")); // 삽화 경계 조회
        var choices = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Choices")); // 선택 버튼 영역 조회
        Assert.Greater(body.min.x, art.max.x); // 본문과 삽화 사이 간격 확인
        Assert.Greater(Mathf.Min(body.min.y, art.min.y), choices.max.y); // 선택지와 본문 겹침 방지 확인
        Object.Destroy(marker.gameObject); // 검사 표식 정리
        Object.Destroy(panel.gameObject); // 검사 캔버스 정리
        yield return new ExitPlayMode(); // 편집 모드 복귀
    } // 배치 검사 종료
} // 검사 클래스 종료
