import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as chatApi from "../api/chatApi";
import type { ChatMessage } from "../types";
import { useAuth } from "../context/AuthContext";
import { useClickOutside } from "../hooks";
import { extractErrorMessage } from "../utils/errorMessage";

function greeting(isAdmin: boolean): ChatMessage {
  return {
    role: "assistant",
    content: isAdmin
      ? "Hi! I can manage products, users, and orders for you."
      : "Hi! I can look up products, check your orders, or cancel one for you.",
  };
}

// Small floating icon, bottom-right, on every page once logged in. The
// SAME backend endpoint (/api/assistant/chat) is used for shoppers and
// admins — the tool set the server exposes differs by role, scoped to
// the caller's own JWT (see Kartly.Application.Services.AssistantService).
export function FloatingAssistant() {
  const { isAuthenticated, isAdmin, user } = useAuth();
  const queryClient = useQueryClient();
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([greeting(isAdmin)]);
  const [input, setInput] = useState("");
  const [isThinking, setIsThinking] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  useClickOutside(panelRef, () => setIsOpen(false));

  // WHY THIS EFFECT EXISTS: this component never unmounts just because
  // it briefly returns null while logged out — React keeps its state
  // alive. Without this, chat history from one account (including
  // anything it discussed about that account's own orders) would still
  // be sitting in memory and visible if a DIFFERENT account logs in on
  // the same browser tab afterward. Resetting on every user id change
  // (including to/from "logged out") closes that gap, matching the same
  // fix applied to React Query's cache in AuthContext.tsx.
  useEffect(() => {
    setMessages([greeting(isAdmin)]);
    setIsOpen(false);
  }, [user?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  if (!isAuthenticated) return null;

  async function send() {
    if (!input.trim() || isThinking) return;
    const history = messages;
    const text = input;
    setMessages((prev) => [...prev, { role: "user", content: text }]);
    setInput("");
    setIsThinking(true);
    try {
      const response = await chatApi.sendChatMessage(text, history);
      setMessages((prev) => [...prev, { role: "assistant", content: response.reply }]);
      if (response.actionsPerformed.length > 0) {
        queryClient.invalidateQueries({ queryKey: ["my-orders"] });
        queryClient.invalidateQueries({ queryKey: ["products"] });
        queryClient.invalidateQueries({ queryKey: ["admin-products"] });
        queryClient.invalidateQueries({ queryKey: ["admin-orders"] });
      }
    } catch (err: any) {
      // WHY THIS MATTERS: "couldn't reach the assistant" used to show for
      // EVERY failure — missing Groq key, invalid key, backend not
      // running, or a real network issue — with no way to tell which.
      // extractErrorMessage() also surfaces the real exception detail
      // when the backend is running in Development (see
      // ExceptionHandlingMiddleware.cs), which is what actually turns
      // this into something actionable instead of a dead end.
      const status = err?.response?.status;
      const fallback = status
        ? `Assistant request failed (HTTP ${status}). Check the backend console for details.`
        : "Couldn't reach the backend at all — is Kartly.API running, and does VITE_ADMIN_API_URL in .env match its URL?";
      setMessages((prev) => [...prev, { role: "assistant", content: `Assistant error: ${extractErrorMessage(err, fallback)}` }]);
    } finally {
      setIsThinking(false);
    }
  }

  return (
    <div className="fixed bottom-5 right-5 z-50">
      {isOpen && (
        <div ref={panelRef} className="mb-3 flex h-96 w-80 flex-col overflow-hidden rounded-2xl border border-[var(--color-line)] bg-[var(--color-surface)] shadow-xl">
          <div className="border-b border-[var(--color-line)] px-4 py-3">
            <p className="font-600 text-[var(--color-ink)]">Kartly Assistant</p>
            <p className="text-xs text-[var(--color-ink-soft)]">{isAdmin ? "Admin tools enabled" : "Ask about products or your orders"}</p>
          </div>
          <div className="flex-1 space-y-2 overflow-y-auto p-3">
            {messages.map((m, i) => (
              <div key={i} className={`flex ${m.role === "user" ? "justify-end" : "justify-start"}`}>
                <div className={`max-w-[85%] rounded-2xl px-3 py-2 text-xs ${m.role === "user" ? "bg-[var(--color-ink)] text-[var(--color-paper)]" : "bg-[var(--color-paper)] text-[var(--color-ink)]"}`}>{m.content}</div>
              </div>
            ))}
            {isThinking && <div className="text-xs text-[var(--color-ink-soft)]">Thinking...</div>}
          </div>
          <div className="flex gap-2 border-t border-[var(--color-line)] p-2">
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && send()}
              placeholder="Type a message..."
              className="flex-1 rounded-full border border-[var(--color-line)] bg-[var(--color-paper)] px-3 py-1.5 text-xs outline-none focus:border-[var(--color-primary)]"
            />
            <button onClick={send} disabled={isThinking} className="rounded-full bg-[var(--color-ink)] px-3 text-xs font-600 text-[var(--color-paper)] disabled:opacity-60">Send</button>
          </div>
        </div>
      )}

      <button
        onClick={() => setIsOpen((o) => !o)}
        aria-label="Open AI assistant"
        className="flex h-14 w-14 items-center justify-center rounded-full bg-[var(--color-ink)] text-[var(--color-paper)] shadow-lg transition hover:bg-[var(--color-primary)]"
      >
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" />
        </svg>
      </button>
    </div>
  );
}
