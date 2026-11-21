using System; // 실행 인자와 실패 메시지 사용
using System.Collections; // 실제 프레임과 원화 로드 대기
using System.Collections.Generic; // 텍스처 식별자 중복 제거
using UnityEngine; // 실제 플레이어 화면과 메모리 계측
using UnityEngine.Profiling; // 로드된 텍스처의 네이티브 메모리 조회
using UnityEngine.UI; // 실제 도감 버튼과 스크롤 조작

#if UNITY_STANDALONE_WIN // Windows 플레이어의 선택적 측정 코드
internal sealed class CodexMemoryProbe : MonoBehaviour // 도감 목록과 상세의 네이티브 텍스처 메모리 측정
{ // 도감 계측 시작
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Windows 게임 시작 뒤 계측 요청 확인
    private static void StartWhenRequested() // 실행 인자가 있는 경우에만 독립 도감 열기
    { // 실행 인자 확인 시작
        string[] args = Environment.GetCommandLineArgs(); // Windows 게임에 전달된 실제 실행 인자
        int index = Array.IndexOf(args, "-codex-memory-probe"); // 선택적 계측 실행 인자 위치
        if (index < 0) // 일반 게임 실행 확인
        { // 일반 실행 처리 시작
            return; // 평소 실행에서 계측 오브젝트 미생성
        } // 일반 실행 처리 종료
        int category = 7; // 기본 카드 도감 선택
        if (index + 1 < args.Length && int.TryParse(args[index + 1], out int requested)) // 실행 인자에 지정된 분류 확인
        { // 분류 선택 시작
            category = requested; // 명시한 도감 분류 번호 사용
        } // 분류 선택 종료
        Application.runInBackground = true; // 활성 창이 아닌 계측에서도 실제 프레임 진행 유지
        var host = new GameObject("CodexMemoryProbe", typeof(CodexMemoryProbe)); // 실제 렌더 프레임과 독립 계측 동작 생성
        DontDestroyOnLoad(host); // 시작 씬의 전환 중에도 계측 유지
        host.GetComponent<CodexMemoryProbe>().StartCoroutine(Measure(category)); // 분류의 실제 도감 화면과 텍스처 측정 시작
    } // 실행 인자 확인 종료

    private static IEnumerator Measure(int category) // 시작과 목록 및 상세의 실제 메모리 표본 수집
    { // 표본 수집 시작
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed); // 비교 표본의 고정 Windows 화면 크기
        yield return null; // 첫 씬의 시작과 해상도 반영 대기
        var root = new GameObject("CodexMemoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 독립 렌더 캔버스 생성
        DontDestroyOnLoad(root); // 시작 씬이 이동해도 표본 화면 유지
        var canvas = root.GetComponent<Canvas>(); // 실제 도감 렌더링 캔버스
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 일반 2D 화면과 같은 렌더 방식
        canvas.sortingOrder = 500; // 시작 화면보다 위에 계측 도감 표시
        var scaler = root.GetComponent<CanvasScaler>(); // 도감 UI의 고정 기준 해상도
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 크기와 실제 화면 크기 연결
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기본 도감과 같은 배치 단위 사용
        var view = CodexView.Open(canvas); // 실제 게임의 도감 화면 초기화
        if (view == null || view.transform.Find("Categories/Category_" + category) == null) // 측정할 분류와 도감 버튼 확인
        { // 실행 입력 실패 처리 시작
            Finish(1, "missing category " + category); // 잘못된 계측 분류의 종료 상태 기록
            yield break; // 유효하지 않은 분류의 표본 수집 중단
        } // 실행 입력 실패 처리 종료
        yield return Resources.UnloadUnusedAssets(); // 사용 중인 시작 화면 자산과 공용 텍스처만 남긴 기준선
        var baseline = ReadTextures("before", category); // 원화 목록을 열기 전 실제 텍스처 원본 상태
        int baselineWrappers = Resources.FindObjectsOfTypeAll<CodexArtworkResource>().Length; // 이미 로드된 원화 참조 기준 수량
        view.transform.Find("Categories/Category_" + category).GetComponent<Button>().onClick.Invoke(); // 원래 도감 분류 버튼으로 목록 열기
        Canvas.ForceUpdateCanvases(); // 실제 목록 표시 범위 갱신
        yield return null; // 텍스처가 배치된 첫 렌더 프레임 대기
        var list = view.transform.Find("ListPanel"); // 현재 도감 분류의 실제 목록
        var scroll = list.GetComponent<ScrollRect>(); // 재사용 썸네일을 다시 연결할 실제 스크롤
        var firstList = ReadTextures("first-list", category); // 첫 화면의 전체 및 썸네일 네이티브 메모리
        scroll.verticalNormalizedPosition = 0f; // 실제 목록 스크롤의 끝으로 이동
        scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 사용 중인 재사용 셀 갱신 경로 실행
        Canvas.ForceUpdateCanvases(); // 마지막 화면의 표시 위치 반영
        yield return null; // 스크롤 뒤 실제 렌더 프레임 대기
        var beforeDetail = ReadTextures("scrolled-list", category); // 상세를 열기 전 스크롤 후 이미지 집계
        if (firstList.OriginalWrappers != baselineWrappers || beforeDetail.OriginalWrappers != baselineWrappers) // 목록에서 원화 참조가 새로 로드됐는지 확인
        { // 원화 로딩 순서 실패 처리 시작
            Finish(1, "original wrapper loaded from list"); // 상세 전에 원화 로드가 발생한 회귀 기록
            yield break; // 잘못된 로딩 순서의 표본 중단
        } // 원화 로딩 순서 실패 처리 종료
        Transform cell = FirstVisibleArtworkCell(scroll.content); // 현재 실제로 보이는 첫 이미지 항목 조회
        if (cell == null) // 끝 행에 원화가 없는 항목만 배치된 경우 확인
        { // 첫 화면 대체 표본 시작
            scroll.verticalNormalizedPosition = 1f; // 실제 목록의 첫 화면으로 복귀
            scroll.onValueChanged.Invoke(scroll.normalizedPosition); // 재사용 셀에 상단 표시 자료 연결
            yield return null; // 첫 화면 재표시 대기
            cell = FirstVisibleArtworkCell(scroll.content); // 첫 화면의 실제 이미지 표본 조회
            beforeDetail = ReadTextures("return-list", category); // 실제 상세 선택 직전의 주소와 텍스처 확인
        } // 첫 화면 대체 표본 종료
        if (cell == null) // 실제 텍스처가 있는 선택 표본 확인
        { // 분류 표본 누락 처리 시작
            Finish(1, "no visible artwork in category " + category); // 썸네일이 없는 분류의 확정 실패 기록
            yield break; // 상세 원화 표본 없는 계측 중단
        } // 분류 표본 누락 처리 종료
        var thumbnail = ReadImage(cell.Find("Image").GetComponent<Image>()); // 선택 목록 이미지의 크기와 네이티브 바이트
        cell.GetComponent<Button>().onClick.Invoke(); // 실제 도감 셀 버튼으로 상세 열기
        var detail = view.transform.Find("Detail/Artwork")?.GetComponent<Image>(); // 현재 선택한 상세의 원화 화면 조회
        float deadline = Time.realtimeSinceStartup + 15f; // Windows 플레이어 원화 준비 최대 대기 시간
        while (detail != null && detail.sprite == null && Time.realtimeSinceStartup < deadline) // 원화 비동기 완료 대기
        { // 실제 프레임 진행 시작
            yield return null; // 렌더 프레임 동안 원화 확인
        } // 실제 프레임 진행 종료
        if (detail == null || detail.sprite == null) // 새로 고른 상세 원화 실제 표시 확인
        { // 상세 원화 실패 처리 시작
            Finish(1, "detail artwork did not load"); // 원화 표시 누락을 결과에 기록
            yield break; // 다음 단계로 가지 않는 비교 불가 표본
        } // 상세 원화 실패 처리 종료
        var full = ReadImage(detail); // 선택한 실제 원화의 텍스처 크기와 네이티브 바이트
        var opened = ReadTextures("detail-open", category); // 상세에 추가된 네이티브 텍스처 표본
        Debug.Log($"[CodexMemory] selected category={category} thumbId={thumbnail.Id} thumbSize={thumbnail.Width}x{thumbnail.Height} thumbNativeBytes={thumbnail.Bytes} originalId={full.Id} originalSize={full.Width}x{full.Height} originalNativeBytes={full.Bytes} originalInBaseline={baseline.Ids.Contains(full.Id)} originalInFirstList={firstList.Ids.Contains(full.Id)} originalBeforeDetail={beforeDetail.Ids.Contains(full.Id)} detailWrapperDelta={opened.OriginalWrappers - beforeDetail.OriginalWrappers}"); // 원본이 도감 밖에서 이미 로드된 경우도 구분한 선택 표본
        view.transform.Find("Detail/CloseButton").GetComponent<Button>().onClick.Invoke(); // 실제 상세 닫기 버튼 실행
        yield return null; // 상세 창 파괴와 참조 해제 반영 대기
        ReadTextures("detail-closed-before-sweep", category); // 소유한 참조만 닫은 직후의 실제 로드 상태
        yield return Resources.UnloadUnusedAssets(); // 미사용 원화만 회수한 별도 비교 조건
        ReadTextures("detail-closed-after-sweep", category); // 공용 사용과 회수 가능성을 구분할 마지막 표본
        bool separate = thumbnail.Id != full.Id && Math.Max(thumbnail.Width, thumbnail.Height) <= 128 && (category == 4 || opened.OriginalWrappers == beforeDetail.OriginalWrappers + 1); // 별도 텍스처와 선택 원화 한 장 요청 확인
        Finish(separate ? 0 : 1, separate ? "complete" : "textures were not separate"); // 자산의 분리 여부를 Windows 종료 코드로 전달
    } // 표본 수집 종료

    private static Transform FirstVisibleArtworkCell(Transform grid) // 표시 영역에서 그림이 연결된 실제 셀 선택
    { // 표시 항목 조회 시작
        foreach (Transform cell in grid) // 현재 재사용된 목록의 셀 순회
        { // 실제 셀 이미지 확인 시작
            if (cell.gameObject.activeInHierarchy && cell.Find("Image")?.GetComponent<Image>().sprite != null) // 표시 영역으로 들어온 실제 썸네일 확인
            { // 계측 항목 확정 시작
                return cell; // 표본을 열 목록 셀 반환
            } // 계측 항목 확정 종료
        } // 실제 셀 이미지 확인 종료
        return null; // 표시할 그림이 없는 분류 상태 반환
    } // 표시 항목 조회 종료

    private static (int Id, int Width, int Height, long Bytes) ReadImage(Image image) // 원화 참조를 남기지 않는 이미지 표본
    { // 이미지 메모리 조회 시작
        Texture2D texture = image.sprite.texture; // 표시 중인 실제 스프라이트 텍스처 확인
        return (texture.GetInstanceID(), texture.width, texture.height, Profiler.GetRuntimeMemorySizeLong(texture)); // 실제 이미지 객체 식별자와 네이티브 메모리 기록
    } // 이미지 메모리 조회 종료

    private static (HashSet<int> Ids, int OriginalWrappers) ReadTextures(string stage, int category) // 살아 있는 텍스처들의 중복 없는 네이티브 바이트 집계
    { // 텍스처 표본 조회 시작
        var ids = new HashSet<int>(); // 원화가 먼저 로드되어 있었는지 비교할 식별자 목록
        int thumbs = 0; // 살아 있는 도감 썸네일 텍스처 수
        long textureBytes = 0; // 모든 텍스처 네이티브 바이트 합계
        long thumbBytes = 0; // 도감 썸네일 전용 텍스처의 네이티브 바이트 합계
        foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) // 이미 로드된 텍스처만 조회
        { // 텍스처 메모리 합산 시작
            ids.Add(texture.GetInstanceID()); // 원화 사전 로딩 확인용 식별자 수집
            long size = Profiler.GetRuntimeMemorySizeLong(texture); // 네이티브 객체별 실제 보고 바이트
            textureBytes += size; // 씬과 UI를 포함한 전체 텍스처 메모리
            if (texture.name.StartsWith("CodexThumb_", StringComparison.Ordinal)) // 도감 전용 썸네일 자산만 선택
            { // 도감 전용 메모리 합산 시작
                thumbs++; // 고유 썸네일 텍스처 수 증가
                thumbBytes += size; // 도감 전용 네이티브 바이트 집계
            } // 도감 전용 메모리 합산 종료
        } // 텍스처 메모리 합산 종료
        int originals = Resources.FindObjectsOfTypeAll<CodexArtworkResource>().Length; // 상세 원화 래퍼의 실제 로드 수
        Debug.Log($"[CodexMemory] stage={stage} category={category} screen={Screen.width}x{Screen.height} textures={ids.Count} textureNativeBytes={textureBytes} thumbnailTextures={thumbs} thumbnailNativeBytes={thumbBytes} originalWrappers={originals}"); // Windows 플레이어의 분류와 단계별 메모리 결과 출력
        return (ids, originals); // 원화 대비를 위한 식별자와 래퍼 수만 보관
    } // 텍스처 표본 조회 종료

    private static void Finish(int exitCode, string result) // Windows 로그와 실행 종료 상태 기록
    { // 계측 종료 시작
        Debug.Log($"[CodexMemory] result={result} exitCode={exitCode}"); // 계측 성공 또는 실패 사유 기록
        Application.Quit(exitCode); // 계측 실행을 배치 수집 명령에 반환
    } // 계측 종료 완료
} // 도감 계측 종료
#endif // Windows 플레이어의 선택적 측정 코드 종료
