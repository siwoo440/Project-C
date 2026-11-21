using System; // 표본 정렬과 이름 비교
using System.Collections; // 실제 UI 프레임 대기
using System.Collections.Generic; // 생성한 셀과 시간 표본 기록
using System.Linq; // 중앙값과 자료량 계산
using System.Reflection; // 비교용 대용량 목록에만 기존 재구성 경로 연결
using NUnit.Framework; // 계측 상태 검증
using UnityEngine; // 같은 화면 조건 설정
using UnityEngine.TestTools; // 플레이 모드 계측
using UnityEngine.UI; // 실제 도감 버튼과 스크롤 조작
using Stopwatch = System.Diagnostics.Stopwatch; // 고해상도 경과 시간 측정
using Object = UnityEngine.Object; // 계측용 화면 정리

public sealed class CodexScrollMeasurementTests // 동일 조작의 도감 셀 생성량과 동기 처리 시간 측정
{ // 계측 시작
    [UnityTest] // 한 화면 크기에서 세 번 반복
    public IEnumerator MeasureRealRelicsAndSynthetic549() // 실제 등록된 유물과 대용량 비교용 행 계측
    { // 표본 계측 시작
        yield return new EnterPlayMode(); // 실제 캔버스 배치와 파괴 동작 활성화
        yield return Measure(); // 전환 완료 후 계측 자료와 화면 생성
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 플레이 모드 전환 종료
    private static IEnumerator Measure() // 실제 플레이 모드에서 도감 계측
    { // 표본 수집 시작
        var root = new GameObject("CodexMeasure", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 독립 측정 화면 생성
        var canvas = root.GetComponent<Canvas>(); // 실제 도감이 사용할 캔버스
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 크기에 맞춘 2D 표시
        var scaler = root.GetComponent<CanvasScaler>(); // 서로 다른 구현에 같은 기준 해상도 적용
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 고정 기준 해상도 사용
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 도감의 여섯 열 기준 크기
        scaler.matchWidthOrHeight = 0.5f; // 가로와 세로 기준을 같은 비율로 적용
        yield return null; // 캔버스 배치와 글꼴 준비
        var real = CodexData.Load(3); // 실제 유물 자료와 원화
        Assert.Greater(real.Count, 0); // 실물 비교 원본 확인
        var synthetic = Enumerable.Range(0, 549).Select(i => real[i % real.Count].WithName("계측 " + i.ToString("D3"))).ToList(); // 원화를 미리 불러오지 않는 비교용 549개 행
        foreach (bool useSynthetic in new[] { false, true }) // 실제 자료와 비교 자료를 각각 측정
        { // 자료별 계측 시작
            var initial = new List<double>(); // 목록 첫 표시 표본
            var scrolling = new List<double>(); // 여덟 번 스크롤 표본
            var sorting = new List<double>(); // 네 번 정렬 표본
            for (int run = 0; run < 4; run++) // 첫 회 예열과 기록할 세 회 반복
            { // 측정 회차 시작
                var view = CodexView.Open(canvas); // 매 회 새 도감 열기
                Assert.IsNotNull(view); // 이전 회차 도감 파괴 확인
                Canvas.ForceUpdateCanvases(); // 도감 공통 화면 준비를 측정에서 분리
                var list = view.transform.Find("ListPanel"); // 실제 목록 패널
                var scroll = list.GetComponent<ScrollRect>(); // 실제 도감 스크롤
                var grid = scroll.content; // 실제 격자
                var sort = list.Find("SortButton").GetComponent<Button>(); // 실제 정렬 버튼
                var open = view.transform.Find("Categories/Category_3").GetComponent<Button>(); // 실제 유물 분류 버튼
                var counts = new HashSet<int>(); // 중복 없이 생성된 셀 객체 식별자 기록
                var watch = Stopwatch.StartNew(); // 동기 화면 구성 측정 시작
                if (useSynthetic) // 가상 자료 계측 확인
                { // 대용량 표본 구성 시작
                    list.gameObject.SetActive(true); // 실제 표시 영역 활성화
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic; // 기존 도감 내부에서 자료만 대체
                    typeof(CodexView).GetField("entries", flags).SetValue(view, new List<CodexEntry>(synthetic)); // 매 회 같은 순서의 비교용 목록 공급
                    typeof(CodexView).GetMethod("RebuildEntries", flags).Invoke(view, null); // 기존과 변경본의 동일 목록 재구성 호출
                } // 대용량 표본 구성 종료
                else // 실제 등록 자료 계측
                { // 정식 분류 열기 시작
                    open.onClick.Invoke(); // 원래 데이터 조회와 정렬을 포함한 유물 분류 열기
                } // 정식 분류 열기 종료
                Canvas.ForceUpdateCanvases(); // 첫 화면의 동기 배치와 텍스트 반영
                watch.Stop(); // 첫 표시 측정 종료
                double initialMs = watch.Elapsed.TotalMilliseconds; // 첫 화면 경과 시간
                Collect(grid, counts); // 첫 화면의 실제 셀 객체 수
                int initialCells = counts.Count; // 첫 표시까지 생성한 셀 수
                watch.Reset(); // 스크롤 동기 처리 누적 측정 준비
                foreach (float position in new[] { 0.75f, 0.5f, 0.25f, 0f, 0.25f, 0.5f, 0.75f, 1f }) // 첫 행에서 마지막 행까지 두 방향 이동
                { // 스크롤 표본 시작
                    watch.Start(); // 스크롤 계산 시작
                    scroll.verticalNormalizedPosition = position; // 실제 스크롤 내용 위치 이동
                    scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 드래그와 같은 위치 변경 신호
                    Canvas.ForceUpdateCanvases(); // 해당 위치의 셀과 텍스트 반영
                    watch.Stop(); // 현재 위치의 동기 처리 측정 종료
                    Collect(grid, counts); // 계측 시간 밖에서 생성 셀 누적
                    yield return null; // 다음 실제 프레임 대기
                } // 스크롤 표본 종료
                double scrollMs = watch.Elapsed.TotalMilliseconds; // 여덟 번 스크롤의 누적 시간
                int scrollCells = counts.Count - initialCells; // 스크롤하면서 추가 생성한 셀 수
                watch.Reset(); // 네 번 정렬의 동기 처리 누적 시작
                for (int index = 0; index < 4; index++) // 이름순과 이름 역순을 두 번 왕복
                { // 정렬 표본 시작
                    watch.Start(); // 정렬 처리 시작
                    sort.onClick.Invoke(); // 기존 정렬 버튼 실제 사용
                    Canvas.ForceUpdateCanvases(); // 바뀐 이름과 그림의 동기 배치 반영
                    watch.Stop(); // 정렬 처리 측정 종료
                    Collect(grid, counts); // 계측 시간 밖에서 새 셀 식별자 수집
                    yield return null; // 이전 셀 제거와 실제 프레임 처리 대기
                } // 정렬 표본 종료
                if (run > 0) // 첫 회 예열 결과 제외
                { // 표본 기록 시작
                    initial.Add(initialMs); // 첫 표시 표본 저장
                    scrolling.Add(scrollMs); // 스크롤 표본 저장
                    sorting.Add(watch.Elapsed.TotalMilliseconds); // 정렬 표본 저장
                    Debug.Log($"[Codex231] sample={run} entries={(useSynthetic ? synthetic.Count : real.Count)} viewport={scroll.viewport.rect.height:F1} initialCells={initialCells} scrollNew={scrollCells} sortNew={counts.Count - initialCells - scrollCells}"); // 화면 규모와 생성량 기록
                } // 표본 기록 종료
                view.gameObject.SetActive(false); // 실제 뒤로 가기와 같은 프레임에 화면과 검색에서 제외
                Object.Destroy(view.gameObject); // 이번 계측 화면 해제
                yield return null; // 다음 표본 이전 화면 제거 대기
            } // 측정 회차 종료
            Debug.Log($"[Codex231] median entries={(useSynthetic ? synthetic.Count : real.Count)} screen={Screen.width}x{Screen.height} openMs={initial.OrderBy(x => x).ElementAt(1):F3} scroll8Ms={scrolling.OrderBy(x => x).ElementAt(1):F3} sort4Ms={sorting.OrderBy(x => x).ElementAt(1):F3}"); // 세 표본의 중앙값과 실행 해상도 기록
        } // 자료별 계측 종료
        Object.Destroy(root); // 계측 전용 캔버스 해제
        yield return null; // 독립 화면 해제 대기
    } // 표본 계측 종료
    private static void Collect(Transform grid, HashSet<int> visited) // 실제 항목 셀 객체만 집계
    { // 셀 집계 시작
        foreach (Transform child in grid) // 현재 목록 소유 셀 순회
        { // 개별 셀 집계 시작
            visited.Add(child.GetInstanceID()); // 여러 정렬에서 재사용된 동일 객체의 중복 집계 방지
        } // 개별 셀 집계 종료
    } // 셀 집계 종료
} // 계측 종료
