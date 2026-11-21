using System.Collections.Generic; // 현재 화면의 원문과 동적 변경 표시
using TMPro; // 게임의 실제 한글과 영어 표시 컴포넌트
using UnityEngine; // 살아 있는 씬의 표시만 갱신
using UnityEngine.SceneManagement; // 새 화면에서도 현재 언어 적용
using UnityEngine.UI; // 기존 화면 Text 및 사용자 입력 제외

internal sealed class ProjectCLanguageDisplay : MonoBehaviour // 화면과 권위 있는 원문을 분리하는 표시 연결
{ // 현재 게임 언어 연결 시작
    private static ProjectCLanguageDisplay instance; // 씬 전환을 넘어 현재 표시를 관리하는 단일 컴포넌트
    private readonly Dictionary<Component, Caption> translated = new Dictionary<Component, Caption>(); // 표시 컴포넌트별 원문과 실제 영어 기록
    private readonly HashSet<TMP_Text> queued = new HashSet<TMP_Text>(); // TMP 렌더 이벤트에서 바뀐 해당 항목만 예약
    private readonly List<TMP_Text> changed = new List<TMP_Text>(); // 같은 프레임의 실제 한글 변경 대기열
    private readonly List<Component> retired = new List<Component>(); // 닫은 창과 이전 전투 씬의 끝난 표시 자료
    private bool needsSceneScan; // 씬에서 처음 켜진 정적 버전의 한 번 갱신

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 실제 게임 시작에 표시 언어 연결 설치
    private static void Install() // 현재 로컬 설정을 쓸 실제 씬 독립 배치
    { // 새 게임 언어 표시 설치 시작
        if (instance != null) // 도메인 재사용과 기존 언어 표시 관리 확인
        { // 이미 있는 표시 관리 처리 시작
            return; // 플레이 중 중복 이벤트 등록 방지
        } // 이미 있는 표시 관리 처리 종료
        var host = new GameObject(nameof(ProjectCLanguageDisplay)); // 씬과 분리해 유지할 실제 언어 연결 오브젝트
        DontDestroyOnLoad(host); // 새로운 전투와 로비에도 이어지는 언어 설정
        instance = host.AddComponent<ProjectCLanguageDisplay>(); // UI와 현재 선택을 잇는 런타임 컴포넌트
    } // 새 게임 언어 표시 설치 종료

    internal static void RefreshCurrent() // 설정에서 클릭한 순간 열린 화면의 실제 표시 갱신
    { // 현재 씬 초기 언어 적용 시작
        if (instance != null) // 앱 시작 이전과 실제 화면이 있는 변경 구분
        { // 열린 씬의 즉시 갱신 시작
            instance.RefreshVisibleCopy(); // 한국어 복원 또는 영어의 실제 화면 적용
        } // 열린 씬의 즉시 갱신 종료
    } // 현재 씬 초기 언어 적용 종료

    internal static void SetCaption(TMP_Text target, string source) // 크기 계산 전에 번역이 필요한 실제 UI 문구
    { // 즉시 표시할 문구 갱신 시작
        if (instance != null && ProjectCLanguage.UseEnglish && instance.translated.TryGetValue(target, out Caption current) && current.Original == source && current.English == target.text) // 이미 같은 원문으로 번역한 화면인지 확인
        { // 같은 원문의 반복 표시 방지 시작
            return; // 기존 영어 메시와 배치 재사용
        } // 같은 원문의 반복 표시 방지 종료
        target.text = source; // 한국어로 돌아갈 수 있도록 화면 원문 할당
        if (instance != null && ProjectCLanguage.UseEnglish) // 영문 크기 측정이 필요한 현재 설정 확인
        { // 영어 문구 즉시 갱신 시작
            instance.UpdateCaption(target); // 원문을 보관한 뒤 현재 영어 표시 적용
        } // 영어 문구 즉시 갱신 종료
    } // 즉시 표시할 문구 갱신 종료

    private void OnEnable() // TMP와 실제 씬의 표시 변경 감시 시작
    { // 게임 텍스트 감시 등록 시작
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(QueueChangedText); // 게임이 갱신한 TMP 표시 항목만 기록
        SceneManager.sceneLoaded += OnSceneLoaded; // 새 화면이 만들어질 때 한 번 정적 조회
        needsSceneScan = ProjectCLanguage.UseEnglish; // 첫 씬 열리기 전 저장 설정이 적용된 경우 확인
    } // 게임 텍스트 감시 등록 종료

    private void OnDisable() // 플레이 모드 종료와 화면 감시 해제
    { // 표시 변화 감시 제거 시작
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(QueueChangedText); // 종료한 게임에서 TMP 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded; // 씬 전환에 설정이 두 번 적용되지 않도록 해제
    } // 표시 변화 감시 제거 종료

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 실제 새 씬의 기존 문구 준비
    { // 새 씬에서 언어 확인 시작
        needsSceneScan = ProjectCLanguage.UseEnglish; // 새 GameObject 생성이 끝난 다음 프레임에 검사
    } // 새 씬에서 언어 확인 종료

    private void QueueChangedText(Object source) // TMP 메시가 다시 배치된 컴포넌트 등록
    { // 한글 원문 동적 변경의 읽기 전용 처리 시작
        if (ProjectCLanguage.UseEnglish && source is TMP_Text text && ProjectCLanguage.ContainsKorean(text.text)) // 영어 중 한글이 새로 할당된 해당 텍스트 확인
        { // 원래 UI를 현재 언어로 다시 그릴 예약 시작
            queued.Add(text); // TMP 렌더링 콜백 안의 메시 재생성을 피한 기록
        } // 원래 UI를 현재 언어로 다시 그릴 예약 종료
    } // 한글 원문 동적 변경의 읽기 전용 처리 종료

    private void LateUpdate() // 실제 UI가 값을 한번 바꾼 뒤 해당 항목 번역
    { // 현재 프레임 변경 문구 반영 시작
        if (needsSceneScan) // 새 씬에서만 정적 문구 수집 여부 확인
        { // 첫 영어 화면의 원문 복사 시작
            needsSceneScan = false; // 다음 프레임의 전수 검색 종료
            RefreshVisibleCopy(); // 배치가 끝난 현재 씬 문구의 실제 영어 적용
        } // 첫 영어 화면의 원문 복사 종료
        if (queued.Count == 0) // 각 프레임의 전체 캔버스 재검색 대신 실제 변경 유무 확인
        { // 변경 없는 경로 시작
            return; // 기존 화면 표시 유지
        } // 변경 없는 경로 종료
        changed.AddRange(queued); // 적용 중 새 UI 콜백이 발생할 수 있어 현재 묶음 복사
        queued.Clear(); // 이번에 처리한 항목의 재등록 허용
        foreach (TMP_Text text in changed) // 실제 바뀐 텍스트만 접근
        { // 동적 UI의 영어 표시 시작
            UpdateCaption(text); // 한국어 데이터에 닿지 않는 해당 라벨에만 적용
        } // 동적 UI의 영어 표시 종료
        changed.Clear(); // 처리한 텍스트 참조 해제
        TrimClosedCaptions(); // 없어진 카드와 창의 참조를 표시 변경 때 함께 정리
    } // 현재 프레임 변경 문구 반영 종료

    private void RefreshVisibleCopy() // 설정 바꾸기 또는 새 씬에 한 번 적용하는 현재 표시
    { // 현재 화면 언어 다시 표시 시작
        if (!ProjectCLanguage.UseEnglish) // 한국어로 되돌린 설정 확인
        { // 기존 한국어 원문 복원 시작
            foreach (var record in translated) // 영어를 실제로 쓴 텍스트만 순회
            { // 변경된 원래 표시 복원 시작
                if (record.Key != null && Read(record.Key) == record.Value.English) // 다른 시스템이 새 값을 쓴 라벨 제외
                { // 이 언어 시스템이 마지막으로 바꾼 표시 대응
                    Write(record.Key, record.Value.Original); // 원래 한국어와 숫자를 그대로 돌려놓기
                } // 이 언어 시스템이 마지막으로 바꾼 표시 종료
            } // 변경된 원래 표시 복원 종료
            translated.Clear(); // 복구한 원래 라벨의 참조 해제
            queued.Clear(); // 이전 언어에서 대기한 동적 UI 적용 취소
            return; // 기본 한국어의 원래 게임 텍스트 경로 종료
        } // 기존 한국어 원문 복원 종료
        TrimClosedCaptions(); // 이전 게임 화면에서 삭제한 라벨들의 자료 정리
        Visit(FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)); // 동적으로 생성된 현대 Unity 표시 조회
        Visit(FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)); // UI의 기존 Text를 같은 설정으로 적용
        Visit(FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None)); // 맵 밖과 탐사 월드 텍스트의 첫 표시
    } // 현재 화면 언어 다시 표시 종료

    private void Visit<T>(T[] captions) where T : Component // 보고 있는 씬에서 대상 표시의 유효성 대조
    { // 실제 게임 표시 항목 방문 시작
        foreach (T caption in captions) // 해당 종류의 현재 실제 표시 객체 순회
        { // 각 표시 애플리케이션 시작
            UpdateCaption(caption); // 현재 한국어 데이터의 표시본만 영어로 변경
        } // 각 표시 애플리케이션 종료
    } // 실제 게임 표시 항목 방문 종료

    private void UpdateCaption(Component target) // 사용자 입력과 실제 게임 텍스트의 분리 적용
    { // 해당 라벨 번역 처리 시작
        if (target == null || IsEditable(target)) // 삭제되거나 사용자가 입력하는 텍스트 확인
        { // 원래 값 보존 경로 시작
            return; // 유저 입력과 존재하지 않는 라벨을 수정하지 않는 경로
        } // 원래 값 보존 경로 종료
        string source = Read(target); // 원래 게임 UI가 전달한 현재 값
        if (translated.TryGetValue(target, out Caption previous) && source == previous.English) // 이미 현지화한 현재 텍스트 확인
        { // 마지막으로 써 둔 영어 상태 시작
            return; // 이전 번역을 새 한국어 원문으로 저장하지 않는 경로
        } // 마지막으로 써 둔 영어 상태 종료
        string english = ProjectCLanguage.Translate(source); // 정확한 고정문과 동적 영어 적용
        if (english == source) // 수치나 원래 영어 및 매칭하지 않은 원문 확인
        { // 실제 글자 변화 없는 경로 시작
            translated.Remove(target); // 새로 들어온 원래 표시와 이전 번역 참조 분리
            return; // 최근 입력이 그대로 표시되는 경우
        } // 실제 글자 변화 없는 경로 종료
        translated[target] = new Caption(source, english); // 두 언어 사이를 오갈 때 쓸 원래 표시 복사
        Write(target, english); // 데이터 에셋 없이 현재 비주얼 글자만 변경
    } // 해당 라벨 번역 처리 종료

    private void TrimClosedCaptions() // 종료된 카드나 씬을 붙드는 이전 표시의 참조 정리
    { // 삭제된 라벨 수집 시작
        foreach (Component target in translated.Keys) // 이전에 표시한 실제 대상 확인
        { // 현재 씬 수명 확인 시작
            if (target == null) // Unity에서 제거된 창 또는 컴포넌트 확인
            { // 이전 참조 해제 예약 시작
                retired.Add(target); // 순회 뒤 지울 실제 Unity 키 저장
            } // 이전 참조 해제 예약 종료
        } // 현재 씬 수명 확인 종료
        foreach (Component target in retired) // 제거된 대상만 사전에서 정리
        { // 닫힌 창 참조 해제 시작
            translated.Remove(target); // 이전 씬 라벨 및 왕복용 복사 값 해제
        } // 닫힌 창 참조 해제 종료
        retired.Clear(); // 삭제한 객체의 임시 목록 비우기
    } // 삭제된 라벨 수집 종료

    private static bool IsEditable(Component target) // 설정 적용 시 플레이어가 입력 중인 값의 보존
    { // 입력 필드와 표시용 안내 문구 확인 시작
        if (target is TMP_Text tmp) // 프로젝트의 일반 입력 필드 확인
        { // 기존 사용자가 작성하는 텍스트 검사 시작
            TMP_InputField tmpInput = tmp.GetComponentInParent<TMP_InputField>(); // 사용자가 편집할 수 있는 실제 TMP 입력 컴포넌트
            return tmpInput != null && tmpInput.textComponent == tmp; // 입력 내용만 제외하고 플레이어 안내는 그대로 번역
        } // 기존 사용자가 작성하는 텍스트 검사 종료
        InputField input = target is Text ? target.GetComponentInParent<InputField>() : null; // 이전 버전 UI의 편집 가능한 원래 입력 칸 조회
        return input != null && input.textComponent == target; // 유니티의 실제 컴포넌트 존재와 대상 입력 값 확인
    } // 입력 필드와 표시용 안내 문구 확인 종료

    private static string Read(Component target) // 현재 각 표시 방식의 원래 문자 조회
    { // 실제 표시 종류에 따른 값 선택 시작
        return target switch // 탐사와 기존 UI의 지원되는 실제 표시 계열
        { // 화면 문자 검색 분기 시작
            TMP_Text tmp => tmp.text, // 전투 및 메인 설정의 현대 TMP 문자열
            Text text => text.text, // 기존 UI 텍스트 문자열
            TextMesh world => world.text, // 탐사 월드 이벤트 표시 문자열
            _ => null // 다른 종류의 현재 표시 제외
        }; // 화면 문자 검색 분기 종료
    } // 실제 표시 종류에 따른 값 선택 종료

    private static void Write(Component target, string value) // 데이터가 아닌 현재 화면에만 표시 기록
    { // 현재 표시용 문장 기록 시작
        switch (target) // 지원 표시 컴포넌트의 출력 경로 선택
        { // 실제 표시 종류 분기 시작
            case TMP_Text tmp: // 현재 게임 대부분의 TMP 표시
                tmp.text = value; // 텍스트 메시가 그릴 실제 영어 또는 한국어
                break; // 다음 표시 종류 생략
            case Text text: // 이전 버전의 Canvas UI 라벨
                text.text = value; // 현재 UI 글자만 갱신
                break; // 다음 표시 종류 생략
            case TextMesh world: // 실제 탐사 월드의 고정 글자
                world.text = value; // 지도 문구의 현재 표시 언어
                break; // 현재 표시 갱신 종료
        } // 실제 표시 종류 분기 종료
    } // 현재 표시용 문장 기록 종료

    private readonly struct Caption // 현재 컨트롤부터 한국어로 복원할 한영 값
    { // 렌더링 복원 값 시작
        public readonly string Original; // 자산과 별개로 보관하는 원래 표시 값
        public readonly string English; // 화면에 마지막으로 적용한 영어 값
        public Caption(string original, string english) // 해당 표시의 실제 두 값 저장
        { // 복원 값 초기화 시작
            Original = original; // 기존 화면이 할당한 한국어
            English = english; // 화면에 적용한 영어
        } // 복원 값 초기화 종료
    } // 렌더링 복원 값 종료
} // 현재 게임 언어 연결 종료
