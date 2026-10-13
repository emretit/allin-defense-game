#import <UIKit/UIKit.h>

extern "C" void AllinUiHaptic(int kind)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (kind < 2) {
            UIImpactFeedbackGenerator *feedback = [[UIImpactFeedbackGenerator alloc]
                initWithStyle:kind == 0 ? UIImpactFeedbackStyleLight : UIImpactFeedbackStyleMedium];
            [feedback prepare];
            [feedback impactOccurred];
        } else {
            UINotificationFeedbackGenerator *feedback = [[UINotificationFeedbackGenerator alloc] init];
            [feedback prepare];
            [feedback notificationOccurred:kind == 2 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError];
        }
    });
}
