using System.Collections; // 플레이 모드에서 실제 언어 전환 대기
using NUnit.Framework; // 설정과 화면의 영어 및 한국어 검증
using TMPro; // 실제 동적 텍스트 구성
using UnityEngine; // 변경된 현재 씬의 화면 라벨 생성
using UnityEngine.TestTools; // 플레이어와 같은 런타임 초기화 실행

public sealed class ProjectCLanguageTests // 저장 데이터 변경 없는 두 언어의 실제 사용자 화면
{ // 언어 회귀 검사 시작
    [Test] // 기본 선택과 서식 정보의 보존 검사
    public void TableTranslatesNamesAndInterpolatedStatusWithoutChangingKoreanValues() // 원본 캐릭터 이름과 현재 상태 카드 서식 확인
    { // 한영 고정문과 동적 문구의 비교 시작
        const string header = "바보 상태 상세"; // 실제 아군 상태의 동적 제목
        const string status = "<color=#FF7777>[실명] 실명 · 디버프</color>"; // 실제 상태 본문의 태그와 한국어
        const string stacked = "실명 · 2/4중첩 · 3T"; // 전투 수치가 들어 있는 상태 상세 목록
        const string detail = "<color=#FF7777>[암] 실명 · 디버프</color>\n공격 명중률 10%p 감소 · 1/3중첩 · 2T"; // 실제 상세창의 머리말과 수치가 이어지는 두 줄
        ProjectCLanguage.Apply(false); // 기존 게임의 기본 한국어 상태 설정
        Assert.AreEqual(header, ProjectCLanguage.Translate(header)); // 기본 설정에서 실제 원문 표시 확인
        try // 영어 확인 뒤 원래 한국어 복원 보장
        { // 실제 고정 및 동적 영어 검사 시작
            ProjectCLanguage.Apply(true); // 영어 설정의 실제 번역 활성화
            Assert.AreEqual("New Game", ProjectCLanguage.Translate("새로하기")); // 실제 시작 메뉴 고정 문구 확인
            Assert.AreEqual("The Fool Status Details", ProjectCLanguage.Translate(header)); // 동적 소유자와 상세 내용의 영어 결합 확인
            Assert.AreEqual("<color=#FF7777>[Blind] Blind · Debuff</color>", ProjectCLanguage.Translate(status)); // 상태 색과 각 표시 값의 영어 적용 확인
            Assert.AreEqual("Blind · 2/4 stacks · 3T", ProjectCLanguage.Translate(stacked)); // 전투 중첩과 턴 숫자 보존 확인
            Assert.AreEqual("<color=#FF7777>[Darkness] Blind · Debuff</color>\nAttack accuracy 10%p reduced · 1/3 stacks · 2T", ProjectCLanguage.Translate(detail)); // 문장별 번역에서 이웃 줄의 마크와 수치 보존 확인
            Assert.AreEqual("8/12", ProjectCLanguage.Translate("8/12")); // 원래 수치나 영문이 전달된 경우 그대로 유지
        } // 실제 고정 및 동적 영어 검사 종료
        finally // 검증 간 서로 다른 언어가 남지 않는 정리
        { // 기본 표시와 원본 복구 시작
            ProjectCLanguage.Apply(false); // 이어질 다른 검증에 기본 한국어 적용
        } // 기본 표시와 원본 복구 종료
        Assert.AreEqual(status, ProjectCLanguage.Translate(status)); // 다시 한국어로 바꾼 뒤 원래 태그와 문구 확인
    } // 한영 고정문과 동적 문구의 비교 종료

    [UnityTest] // 현재 보이는 게임 라벨의 양방향 실제 갱신 검사
    public IEnumerator LanguageSwitchRefreshesAnAlreadyOpenCanvasAndRestoresSource() // 현재 열려 있는 UI를 다시 만들지 않고 언어 왕복
    { // 현재 화면 언어 갱신 검사 시작
        yield return new EnterPlayMode(); // 실제 게임의 씬 언어 감시를 설치하는 모드
        VerifyCurrentCanvas(); // 현재 열린 TMP 문구와 원래 UI의 상태 확인
        yield return new ExitPlayMode(); // 다음 검증의 기본 씬과 표시 상태 복귀
    } // 현재 화면 언어 갱신 검사 종료

    private static void VerifyCurrentCanvas() // 실제 게임에 열린 캔버스의 원문 보존 검사
    { // 독립 레이아웃과 켜져 있는 UI 초기화 시작
        GameObject panel = new GameObject("LanguageLiveCanvas", typeof(RectTransform), typeof(Canvas)); // 현재 게임처럼 열려 있는 실제 설정 캔버스
        var label = PreparationUi.CreateText("ExistingButton", panel.transform, "설정", 24f); // 이미 열려 있던 원래 한글 메뉴
        var secondary = PreparationUi.CreateText("ExistingTooltip", panel.transform, "언어 선택", 24f); // 원문으로 유지할 독립 도움말 문구
        try // 실제 라벨을 리뷰한 뒤 임시 시험 대상 정리
        { // 플레이 모드의 즉시 언어 전환 확인 시작
            ProjectCLanguage.Apply(false); // 기본 설정으로 만들어진 현재 메뉴
            Assert.AreEqual("설정", label.text); // 새 화면에서 원래 한국어 실제 표시 확인
            ProjectCLanguage.Apply(true); // 현재 씬 위에서 영어 설정 활성화
            Assert.AreEqual("Settings", label.text); // 기존 메뉴 텍스트가 같은 객체에서 즉시 영어인지 확인
            Assert.AreEqual("Game Language", secondary.text); // 열린 다른 설명의 일괄 영어 적용 확인
            label.text = "123"; // 게임이 원래 라벨에 나중에 설정한 최신 수치
            ProjectCLanguage.Apply(false); // 로비를 떠나지 않고 한국어로 되돌리기
            Assert.AreEqual("123", label.text); // 이전 한국어 복사가 새 수치를 덮지 않는지 확인
            Assert.AreEqual("언어 선택", secondary.text); // 최근 게임이 값을 바꾸지 않은 다른 원문 복원 확인
        } // 플레이 모드의 즉시 언어 전환 확인 종료
        finally // 현재 씬과 이후 검사에 남을 실제 데이터 정리
        { // 언어 확인용 임시 객체 정리 시작
            ProjectCLanguage.Apply(false); // 기존 한글 상태의 검사 간 보존
            Object.DestroyImmediate(panel); // 실제 검증에서 만든 임시 화면 제거
        } // 언어 확인용 임시 객체 정리 종료
    } // 독립 레이아웃과 켜져 있는 UI 초기화 종료
} // 언어 회귀 검사 종료
