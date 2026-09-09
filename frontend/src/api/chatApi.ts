import { authClient } from "./authClient";
import type { ChatMessage, ChatResponse } from "../types";

// Matches Kartly.API's AssistantController. Available to any logged-in user now.
// so the user's JWT is attached automatically —
// the backend requires the Admin role for this endpoint since the
// assistant can create/update/delete real products.
export async function sendChatMessage(
  message: string,
  history: ChatMessage[]
): Promise<ChatResponse> {
  const { data } = await authClient.post("/assistant/chat", { message, history });
  return data as ChatResponse;
}
