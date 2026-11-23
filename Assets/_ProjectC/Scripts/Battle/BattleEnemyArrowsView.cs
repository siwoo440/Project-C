using System.Collections.Generic; // 예정 화살표 목록 사용
using UnityEngine; // 전투 화면 좌표 사용
using UnityEngine.UI; // 전투 화면 도형 사용
public sealed class BattleEnemyArrowsView : MaskableGraphic // 적 행동 대상 화살표 화면
{ // 화살표 화면 시작
    private struct Arrow // 하나의 적 행동 화살표 정보
    { // 화살표 정보 시작
        public Vector2 Start; // 적 행동 아이콘 위치
        public Vector2 End; // 대상 머리 위치
        public Vector2 Control; // 곡선 높이를 정하는 중간 제어점
        public Color Color; // 공격 종류별 화살표 색상
        public float Thickness; // 화살표 선 두께
        public float HeadSize; // 화살촉 크기
    } // 화살표 정보 종료
    private readonly List<Arrow> arrows = new List<Arrow>(); // 현재 표시 화살표 목록
    private readonly List<Arrow> pending = new List<Arrow>(); // 다음 화면의 화살표 목록
    public int ArrowCount => arrows.Count; // 표시 중인 화살표 수
    public static BattleEnemyArrowsView Create(Canvas parent) // 전투 화면에 화살표 화면 생성
    { // 화살표 화면 생성 시작
        GameObject root = new GameObject("EnemyActionArrows", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(BattleEnemyArrowsView)); // 독립 화살표 화면 생성
        root.transform.SetParent(parent.transform, false); // 실제 전투 캔버스에 연결
        RectTransform area = root.transform as RectTransform; // 전체 화면 영역 조회
        area.anchorMin = Vector2.zero; // 화면 왼쪽 아래 앵커 설정
        area.anchorMax = Vector2.one; // 화면 오른쪽 위 앵커 설정
        area.offsetMin = Vector2.zero; // 화면 왼쪽 아래 여백 제거
        area.offsetMax = Vector2.zero; // 화면 오른쪽 위 여백 제거
        Canvas layer = root.GetComponent<Canvas>(); // 독립 화살표 정렬 화면 조회
        layer.overrideSorting = true; // 전투 배경과 독립된 정렬 설정
        layer.sortingOrder = 410; // 전투 화면 앞과 메뉴 버튼 뒤에 표시
        BattleEnemyArrowsView view = root.GetComponent<BattleEnemyArrowsView>(); // 생성된 화살표 화면 조회
        view.raycastTarget = false; // 캐릭터와 카드 입력 통과 설정
        return view; // 생성한 화살표 화면 반환
    } // 화살표 화면 생성 종료
    public void BeginFrame() // 다음 화면의 화살표 준비
    { // 화살표 준비 시작
        pending.Clear(); // 이전 계산 결과 제거
    } // 화살표 준비 종료
    public void AddArrow(RectTransform actorIcon, RectTransform targetPortrait, bool special, bool focused) // 행동 아이콘과 대상 연결
    { // 화살표 연결 시작
        if (actorIcon == null || targetPortrait == null) // 실제 적과 대상의 화면 확인
        { // 연결 불가 처리 시작
            return; // 화살표 추가 중단
        } // 연결 불가 처리 종료
        Vector3 actorWorld = actorIcon.TransformPoint(actorIcon.rect.center); // 적 행동 아이콘 가운데 위치
        Vector3 targetWorld = targetPortrait.TransformPoint(new Vector3(0f, targetPortrait.rect.yMax - targetPortrait.rect.height * 0.12f, 0f)); // 대상 캐릭터 머리 위치
        Vector2 start = rectTransform.InverseTransformPoint(actorWorld); // 화살표 화면 기준 적 위치
        Vector2 end = rectTransform.InverseTransformPoint(targetWorld); // 화살표 화면 기준 대상 위치
        Rect bounds = rectTransform.rect; // 실제 전투 화면 경계
        const float margin = 24f; // 화살촉 보호 화면 여백
        start.x = Mathf.Clamp(start.x, bounds.xMin + margin, bounds.xMax - margin); // 시작점 가로 화면 경계 제한
        float ceiling = Mathf.Max(bounds.yMin + margin, bounds.yMax - Mathf.Max(48f, bounds.height * 0.1f)); // 화면 위쪽 곡선 높이 제한
        start.y = Mathf.Clamp(start.y, bounds.yMin + margin, ceiling - 8f); // 시작점 세로 화면 경계 제한
        end.x = Mathf.Clamp(end.x, bounds.xMin + margin, bounds.xMax - margin); // 종료점 가로 화면 경계 제한
        end.y = Mathf.Clamp(end.y, bounds.yMin + margin, ceiling - 8f); // 종료점 세로 화면 경계 제한
        float rise = Mathf.Min(70f + pending.Count % 4 * 12f, bounds.height * 0.12f); // 여러 화살표를 구분하는 곡선 높이
        Vector2 control = new Vector2((start.x + end.x) * 0.5f, Mathf.Min(ceiling, Mathf.Max(start.y, end.y) + rise)); // 제한 높이 안에서 포물선 제어점 계산
        Color arrowColor = special ? new Color(1f, 0.55f, 0.12f, focused ? 1f : 0.9f) : new Color(1f, 0.2f, 0.2f, focused ? 1f : 0.85f); // 특수 행동과 일반 행동 색상
        pending.Add(new Arrow { Start = start, End = end, Control = control, Color = arrowColor, Thickness = focused ? 5f : 3f, HeadSize = focused ? 20f : 16f }); // 실제 예정 행동 곡선 화살표 저장
    } // 화살표 연결 종료
    public void EndFrame() // 변경된 화살표만 화면에 반영
    { // 화살표 화면 갱신 시작
        bool changed = arrows.Count != pending.Count; // 화살표 수 변경 확인
        for (int index = 0; !changed && index < arrows.Count; index++) // 기존 화살표 순회
        { // 화살표 변경 비교 시작
            Arrow oldArrow = arrows[index]; // 이전 화살표 정보 조회
            Arrow newArrow = pending[index]; // 현재 화살표 정보 조회
            changed = oldArrow.Start != newArrow.Start || oldArrow.End != newArrow.End || oldArrow.Control != newArrow.Control || oldArrow.Color != newArrow.Color || oldArrow.Thickness != newArrow.Thickness || oldArrow.HeadSize != newArrow.HeadSize; // 위치와 곡선 및 강조 상태 변경 확인
        } // 화살표 변경 비교 종료
        if (!changed) // 표시 내용 변경 여부 확인
        { // 동일 화면 처리 시작
            return; // 불필요한 화면 다시 그리기 방지
        } // 동일 화면 처리 종료
        arrows.Clear(); // 이전 화살표 목록 제거
        arrows.AddRange(pending); // 현재 예정 행동 화살표 저장
        SetVerticesDirty(); // 변경된 화살표 도형 갱신 요청
    } // 화살표 화면 갱신 종료
    public bool TryGetArrow(int index, out Vector2 start, out Vector2 end) // 화면 안 화살표 좌표 조회
    { // 화살표 좌표 조회 시작
        if (index < 0 || index >= arrows.Count) // 요청 순번 범위 확인
        { // 범위 밖 처리 시작
            start = Vector2.zero; // 시작점 기본값 설정
            end = Vector2.zero; // 종료점 기본값 설정
            return false; // 화살표 조회 실패 반환
        } // 범위 밖 처리 종료
        start = arrows[index].Start; // 실제 화살표 시작점 반환
        end = arrows[index].End; // 실제 화살표 종료점 반환
        return true; // 화살표 조회 성공 반환
    } // 화살표 좌표 조회 종료
    public bool TryGetCurvePoint(int index, float progress, out Vector2 point) // 곡선 화살표의 중간 위치 조회
    { // 곡선 위치 조회 시작
        if (index < 0 || index >= arrows.Count) // 요청 순번 범위 확인
        { // 범위 밖 처리 시작
            point = Vector2.zero; // 곡선 위치 기본값 설정
            return false; // 곡선 위치 조회 실패 반환
        } // 범위 밖 처리 종료
        point = Evaluate(arrows[index], Mathf.Clamp01(progress)); // 실제 곡선의 진행 위치 계산
        return true; // 곡선 위치 조회 성공 반환
    } // 곡선 위치 조회 종료
    protected override void OnPopulateMesh(VertexHelper mesh) // 실제 화살표 도형 생성
    { // 화살표 도형 생성 시작
        mesh.Clear(); // 이전 도형 제거
        foreach (Arrow arrow in arrows) // 현재 행동 화살표 순회
        { // 화살표 도형 추가 시작
            if ((arrow.End - arrow.Start).sqrMagnitude < 1f) // 너무 짧은 화살표 확인
            { // 짧은 화살표 처리 시작
                continue; // 잘못된 도형 생성 방지
            } // 짧은 화살표 처리 종료
            AddCurveMesh(mesh, arrow, arrow.Thickness + 3f, arrow.HeadSize + 3f, new Color(0.12f, 0.01f, 0.01f, arrow.Color.a)); // 어두운 곡선 외곽선 추가
            AddCurveMesh(mesh, arrow, arrow.Thickness, arrow.HeadSize, arrow.Color); // 색상이 있는 곡선 화살표 추가
        } // 화살표 도형 추가 종료
    } // 화살표 도형 생성 종료
    private static Vector2 Evaluate(Arrow arrow, float progress) // 이차 곡선의 진행 위치 계산
    { // 곡선 위치 계산 시작
        float remaining = 1f - progress; // 시작점에 가까운 비율 계산
        return remaining * remaining * arrow.Start + 2f * remaining * progress * arrow.Control + progress * progress * arrow.End; // 곡선 보간 위치 반환
    } // 곡선 위치 계산 종료
    private static void AddCurveMesh(VertexHelper mesh, Arrow arrow, float thickness, float headSize, Color color) // 곡선과 화살촉 도형 추가
    { // 곡선 화살표 도형 생성 시작
        const int segments = 20; // 매끄러운 곡선을 위한 선 분할 수
        Vector2 previous = arrow.Start; // 첫 선분의 시작점 저장
        for (int index = 1; index <= segments; index++) // 곡선의 각 선분 순회
        { // 곡선 선분 생성 시작
            Vector2 current = Evaluate(arrow, index / (float)segments); // 현재 곡선 선분의 종료점 계산
            AddLineMesh(mesh, previous, current, thickness, color); // 현재 곡선 선분 추가
            previous = current; // 다음 선분의 시작점 저장
        } // 곡선 선분 생성 종료
        Vector2 direction = (arrow.End - arrow.Control).normalized; // 대상에 닿는 곡선 방향 계산
        Vector2 side = new Vector2(-direction.y, direction.x); // 화살촉의 가로 방향 계산
        Vector2 basePoint = arrow.End - direction * headSize; // 화살촉 밑변 위치 계산
        int first = mesh.currentVertCount; // 화살촉 첫 꼭짓점 순번
        AddVertex(mesh, basePoint - side * headSize * 0.45f, color); // 화살촉 왼쪽 꼭짓점
        AddVertex(mesh, basePoint + side * headSize * 0.45f, color); // 화살촉 오른쪽 꼭짓점
        AddVertex(mesh, arrow.End, color); // 화살촉 끝 꼭짓점
        mesh.AddTriangle(first, first + 1, first + 2); // 화살촉 삼각형 추가
    } // 곡선 화살표 도형 생성 종료
    private static void AddLineMesh(VertexHelper mesh, Vector2 start, Vector2 end, float thickness, Color color) // 곡선의 한 선분 도형 추가
    { // 선분 도형 생성 시작
        Vector2 direction = (end - start).normalized; // 선분 방향 계산
        Vector2 half = new Vector2(-direction.y, direction.x) * (thickness * 0.5f); // 선분의 절반 너비 계산
        int first = mesh.currentVertCount; // 선분 첫 꼭짓점 순번
        AddVertex(mesh, start - half, color); // 선 시작 왼쪽 꼭짓점
        AddVertex(mesh, start + half, color); // 선 시작 오른쪽 꼭짓점
        AddVertex(mesh, end + half, color); // 선 끝 오른쪽 꼭짓점
        AddVertex(mesh, end - half, color); // 선 끝 왼쪽 꼭짓점
        mesh.AddTriangle(first, first + 1, first + 2); // 선분의 첫 삼각형 추가
        mesh.AddTriangle(first, first + 2, first + 3); // 선분의 두 번째 삼각형 추가
    } // 선분 도형 생성 종료
    private static void AddVertex(VertexHelper mesh, Vector2 position, Color color) // 색상 있는 도형 꼭짓점 추가
    { // 도형 꼭짓점 생성 시작
        UIVertex vertex = UIVertex.simpleVert; // 기본 UI 꼭짓점 생성
        vertex.position = position; // 꼭짓점 화면 좌표 설정
        vertex.color = color; // 꼭짓점 표시 색상 설정
        mesh.AddVert(vertex); // 화면 도형에 꼭짓점 추가
    } // 도형 꼭짓점 생성 종료
} // 화살표 화면 종료
