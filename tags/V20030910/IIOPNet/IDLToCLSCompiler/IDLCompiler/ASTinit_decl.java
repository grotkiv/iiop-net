/* Generated By:JJTree: Do not edit this line. ASTinit_decl.java */

package parser;

public class ASTinit_decl extends SimpleNodeWithIdent {
  public ASTinit_decl(int id) {
    super(id);
  }

  public ASTinit_decl(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}