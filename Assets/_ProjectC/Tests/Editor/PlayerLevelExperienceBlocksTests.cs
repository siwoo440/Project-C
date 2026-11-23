using System.Collections; // 플레이 모드 화면 대기 사용
using NUnit.Framework; // 레벨과 경험치 표시 검증 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.TestTools; // 실제 플레이 모드 검사 사용
using UnityEngine.UI; // 경험치 블록 색상 검사 사용
public sealed class PlayerLevelExperienceBlocksTests // 레벨 경험치 블록 검사
{ // 경험치 블록 검사 시작
    [UnityTest] // 실제 레벨 화면 검사 지정
    public IEnumerator BlocksFollowExperienceAndLevelUps() // 경험치와 레벨업에 따른 블록 확인
    { // 경험치 블록 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 시작
        GameObject canvasObject = new GameObject("LevelCanvas", typeof(RectTransform), typeof(Canvas)); // 레벨 화면 테스트 캔버스 생성
        Object.DontDestroyOnLoad(canvasObject); // 씬 전환 중 캔버스 유지
        PlayerLevelConfig config = ScriptableObject.CreateInstance<PlayerLevelConfig>(); // 기본 레벨 설정 생성
        PlayerLevelRunManager manager = PlayerLevelRunManager.EnsureInstance(config); // 실제 레벨 진행 관리자 준비
        manager.BeginBattle(); // 전투 경험치 초기화
        PlayerLevelHudView view = PlayerLevelHudView.Create(canvasObject.GetComponent<Canvas>(), manager); // 실제 레벨 화면 생성
        Transform blocks = view.transform.Find("ExperienceBlocks"); // 실제 경험치 칸 영역 조회
        Assert.IsNotNull(blocks); // 경험치 칸 영역 존재 확인
        Assert.AreEqual(5, blocks.childCount); // 첫 레벨 필요 경험치 다섯 칸 확인
        Assert.AreEqual(0, CountFilled(blocks)); // 시작 경험치 빈 칸 확인
        Assert.AreEqual(0, manager.GainExperience(2)); // 경험치 두 점 획득 확인
        Assert.AreEqual(2, CountFilled(blocks)); // 경험치 두 칸 노란색 표시 확인
        Assert.AreEqual(1, manager.GainExperience(3)); // 경험치 충족 후 레벨업 확인
        Assert.AreEqual(8, blocks.childCount); // 다음 레벨 필요 경험치 여덟 칸 확인
        Assert.AreEqual(0, CountFilled(blocks)); // 레벨업 후 경험치 초기화 확인
        Assert.AreEqual(0, manager.GainExperience(3)); // 다음 레벨 경험치 세 점 획득 확인
        Assert.AreEqual(3, CountFilled(blocks)); // 다음 레벨 경험치 세 칸 표시 확인
        Assert.AreEqual(1, manager.PendingMinorCardChoices); // 레벨업 카드 선택권 유지 확인
        manager.GainExperience(10000); // 높은 레벨 경험치 요구량 검사
        Assert.LessOrEqual(blocks.childCount, 24); // 높은 레벨에서도 제한된 블록 수 확인
        Assert.LessOrEqual(CountFilled(blocks), blocks.childCount); // 실제 범위를 벗어나지 않는 채움 확인
        view.Dispose(); // 레벨 진행 이벤트 해제
        Object.DestroyImmediate(canvasObject); // 테스트 화면 정리
        Object.DestroyImmediate(manager.gameObject); // 테스트 레벨 관리자 정리
        Object.DestroyImmediate(config); // 테스트 레벨 설정 정리
        yield return new ExitPlayMode(); // 실제 전투 화면 플레이 종료
    } // 경험치 블록 검사 종료
    private static int CountFilled(Transform blocks) // 노란 경험치 칸 수 조회
    { // 경험치 칸 계산 시작
        int count = 0; // 채워진 칸 수 초기화
        foreach (Transform block in blocks) // 모든 경험치 칸 순회
        { // 경험치 칸 검사 시작
            Image image = block.GetComponent<Image>(); // 실제 칸 이미지 조회
            Assert.IsFalse(image.raycastTarget); // 경험치 칸의 입력 차단 방지 확인
            if (image.color.r > 0.8f && image.color.g > 0.6f) // 노란색 채움 여부 확인
            { // 채워진 칸 처리 시작
                count++; // 채워진 경험치 칸 증가
            } // 채워진 칸 처리 종료
        } // 경험치 칸 검사 종료
        return count; // 실제 채워진 칸 수 반환
    } // 경험치 칸 계산 종료
} // 레벨 경험치 블록 검사 종료
